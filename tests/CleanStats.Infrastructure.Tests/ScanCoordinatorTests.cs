
using CleanStats.Core.Contracts;
using CleanStats.Core.Model;
using CleanStats.Core.Progress;
using CleanStats.Infrastructure;
using Xunit;

namespace CleanStats.Infrastructure.Tests;

public class ScanCoordinatorTests
{
    public sealed class MultiRunBlockingFileScanner : IFileScanner
    {
        private int _callCount;

        public int CallCount => Volatile.Read(ref _callCount);

        public TaskCompletionSource<bool>[] ScanStarted { get; } =
        [
            new(TaskCreationOptions.RunContinuationsAsynchronously),
        new(TaskCreationOptions.RunContinuationsAsynchronously)
        ];

        public async Task<ScanResult> ScanAsync(
            ScanOptions options,
            IProgress<ScanProgress>? progress,
            CancellationToken cancellationToken)
        {
            int callIndex = Interlocked.Increment(ref _callCount) - 1;

            if (callIndex >= ScanStarted.Length)
            {
                throw new InvalidOperationException(
                    "Se realizaron más escaneos de los esperados.");
            }

            ScanStarted[callIndex].TrySetResult(true);

            await Task.Delay(Timeout.Infinite, cancellationToken);

            return new ScanResult([], [], [], ScanStatus.Completed);
        }
    }

    private sealed class FakeFileScanner : IFileScanner
    {
        private readonly ScanResult _result;
        private readonly FileEntry[] _files;

        public FakeFileScanner(ScanResult result, params FileEntry[] files)
        {
            _result = result ?? throw new ArgumentNullException(nameof(result));
            _files = files ?? throw new ArgumentNullException(nameof(files));
        }

        public Task<ScanResult> ScanAsync(
            ScanOptions options,
            IProgress<ScanProgress>? progress,
            CancellationToken cancellationToken)
        {
            foreach (FileEntry file in _files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                progress?.Report(new FileDiscoveredProgress(file.FilePath));

                var classification = new ClassificationResult(
                    file.Id,
                    ClassificationCategory.Other,
                    new[] { ClassificationReason.NoMatchingRule },
                    ClassificationStatus.Classified
                );

                progress?.Report(
                    new FileProcessedProgress(file, classification)
                );
            }

            return Task.FromResult(_result);
        }
    }


    private sealed class BlockingFileScanner : IFileScanner
    {
        public TaskCompletionSource<bool> ScanStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<ScanResult> ScanAsync(
            ScanOptions options,
            IProgress<ScanProgress>? progress,
            CancellationToken cancellationToken)
        {
            ScanStarted.TrySetResult(true);

            await Task.Delay(Timeout.Infinite, cancellationToken);

            return new ScanResult([], [], [], ScanStatus.Completed);
        }
    }


    [Fact]
    public async Task StartScanAsync_WhenScanSucceeds_UpdatesSummary()
    {
        // Arrange
        string rootPath = Path.GetTempPath();

        FileEntry file1 = new FileEntry(
            Path.Combine(rootPath, "file1.txt"),
            100,
            DateTime.UtcNow,
            DateTime.UtcNow);

        FileEntry file2 = new FileEntry(
            Path.Combine(rootPath, "file2.txt"),
            200,
            DateTime.UtcNow,
            DateTime.UtcNow);

        ScanResult result = new ScanResult(
            [file1, file2],
            [],
            [],
            ScanStatus.Completed);

        var scanner = new FakeFileScanner(result, file1, file2);
        var coordinator = new ScanCoordinator(scanner);

        var options = new ScanOptions(rootPath, [], false);

        // Act
        ScanSummary summary = await coordinator.StartScanAsync(
            options,
            CancellationToken.None);

        // Assert
        Assert.NotNull(coordinator.CurrentResult);

        Assert.Equal(2, coordinator.CurrentResult.Classifications.Count);

        Assert.Contains(
            coordinator.CurrentResult.Classifications,
            classification => classification.FileEntryId == file1.Id
        );

        Assert.Contains(
            coordinator.CurrentResult.Classifications,
            classification => classification.FileEntryId == file2.Id
        );
    }

    [Fact]
    public async Task StartScanAsync_WhenRootPathDoesNotExist_ThrowsAndDoesNotActivateScan()
    {
        // Arrange
        string rootPath = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        var result = new ScanResult([], [], [], ScanStatus.Completed);
        var scanner = new FakeFileScanner(result);
        var coordinator = new ScanCoordinator(scanner);
        var options = new ScanOptions(rootPath, [], false);

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => coordinator.StartScanAsync(options, CancellationToken.None));

        Assert.Null(coordinator.CurrentSummary);
    }

    [Fact]
    public async Task CancelScan_WhenScanIsRunning_CancelsScan()
    {
        // Arrange
        string rootPath = Path.GetTempPath();

        var scanner = new BlockingFileScanner();
        var coordinator = new ScanCoordinator(scanner);
        var options = new ScanOptions(rootPath, [], false);

        // Act
        Task<ScanSummary> scanTask = coordinator.StartScanAsync(
            options,
            CancellationToken.None);

        await scanner.ScanStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        coordinator.CancelScan();

        ScanSummary summary = await scanTask.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        Assert.Equal(ScanStatus.Cancelled, summary.Status);
    }


    [Fact]
    public async Task StartScanAsync_WhenScanFinishes_AllowsAnotherScan()
    {
        // Arrange
        string rootPath = Path.GetTempPath();
        var result = new ScanResult([], [], [], ScanStatus.Completed);
        var scanner = new FakeFileScanner(result);
        var coordinator = new ScanCoordinator(scanner);
        var options = new ScanOptions(rootPath, [], false);

        // Act
        ScanSummary firstSummary = await coordinator.StartScanAsync(
            options, CancellationToken.None);

        ScanSummary secondSummary = await coordinator.StartScanAsync(
            options, CancellationToken.None);

        // Assert
        Assert.Equal(ScanStatus.Completed, firstSummary.Status);
        Assert.Equal(ScanStatus.Completed, secondSummary.Status);
        Assert.NotSame(firstSummary, secondSummary);
        Assert.Same(secondSummary, coordinator.CurrentSummary);
    }


    [Fact]
    public async Task StartScanAsync_WhenAnotherScanIsRunning_ThrowsInvalidOperationException()
    {
        // Arrange
        string rootPath = Path.GetTempPath();
        var scanner = new BlockingFileScanner();
        var coordinator = new ScanCoordinator(scanner);
        var options = new ScanOptions(rootPath, [], false);

        // Act: iniciar el primer escaneo
        Task<ScanSummary> firstScan = coordinator.StartScanAsync(
            options, CancellationToken.None);

        await scanner.ScanStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert: el segundo intento debe fallar
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => coordinator.StartScanAsync(options, CancellationToken.None));

        // Limpiar el primer escaneo
        coordinator.CancelScan();
        ScanSummary summary = await firstScan.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(ScanStatus.Cancelled, summary.Status);
    }


    [Fact]
    public async Task CancelScan_WhenCalledDuringSecondScan_CancelsOnlySecondScan()
    {
        // Arrange
        string rootPath = Path.GetTempPath();
        var options = new ScanOptions(rootPath, [], false);

        var firstScanner = new BlockingFileScanner();
        var firstCoordinator = new ScanCoordinator(firstScanner);

        var firstScan = firstCoordinator.StartScanAsync(
            options, CancellationToken.None);

        await firstScanner.ScanStarted.Task.WaitAsync(
            TimeSpan.FromSeconds(5));

        // Act: cancelar y finalizar el primer escaneo
        firstCoordinator.CancelScan();

        ScanSummary firstSummary = await firstScan.WaitAsync(
            TimeSpan.FromSeconds(5));

        // Iniciar un segundo escaneo con su propio coordinador
        var secondScanner = new BlockingFileScanner();
        var secondCoordinator = new ScanCoordinator(secondScanner);

        var secondScan = secondCoordinator.StartScanAsync(
            options, CancellationToken.None);

        await secondScanner.ScanStarted.Task.WaitAsync(
            TimeSpan.FromSeconds(5));

        // Cancelar el segundo escaneo
        secondCoordinator.CancelScan();

        ScanSummary secondSummary = await secondScan.WaitAsync(
            TimeSpan.FromSeconds(5));

        // Assert
        Assert.Equal(ScanStatus.Cancelled, firstSummary.Status);
        Assert.Equal(ScanStatus.Cancelled, secondSummary.Status);

        // El primer coordinador debe conservar su propio resumen
        Assert.Same(firstSummary, firstCoordinator.CurrentSummary);
        Assert.Same(secondSummary, secondCoordinator.CurrentSummary);
        Assert.NotSame(firstSummary, secondSummary);
    }


    [Fact]
    public async Task CancelScan_WhenCalledOnSecondScan_CancelsOnlySecondScan()
    {
        // Arrange
        string rootPath = Path.GetTempPath();
        var options = new ScanOptions(rootPath, [], false);

        var scanner = new MultiRunBlockingFileScanner();
        var coordinator = new ScanCoordinator(scanner);

        // Primer escaneo
        Task<ScanSummary> firstTask = coordinator.StartScanAsync(
            options, CancellationToken.None);

        await scanner.ScanStarted[0].Task.WaitAsync(
            TimeSpan.FromSeconds(5));

        coordinator.CancelScan();

        ScanSummary firstSummary = await firstTask.WaitAsync(
            TimeSpan.FromSeconds(5));

        Assert.Equal(ScanStatus.Cancelled, firstSummary.Status);
        Assert.Same(firstSummary, coordinator.CurrentSummary);

        // Segundo escaneo, con el mismo coordinador y scanner
        Task<ScanSummary> secondTask = coordinator.StartScanAsync(
            options, CancellationToken.None);

        await scanner.ScanStarted[1].Task.WaitAsync(
            TimeSpan.FromSeconds(5));

        Assert.NotSame(firstSummary, coordinator.CurrentSummary);

        coordinator.CancelScan();

        ScanSummary secondSummary = await secondTask.WaitAsync(
            TimeSpan.FromSeconds(5));

        // Assert
        Assert.Equal(ScanStatus.Cancelled, secondSummary.Status);
        Assert.Same(secondSummary, coordinator.CurrentSummary);
        Assert.NotSame(firstSummary, secondSummary);
        Assert.Equal(2, scanner.CallCount);
    }


}
