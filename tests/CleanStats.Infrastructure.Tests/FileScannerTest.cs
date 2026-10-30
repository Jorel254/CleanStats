
using CleanStats.Core.Contracts;
using CleanStats.Core.Model;
using CleanStats.Core.Progress;
using CleanStats.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.AccessControl;
using System.Security.Principal;
using Xunit;

namespace CleanStats.Infrastructure.Tests;

public sealed class FileScannerTest
{
    private readonly FileScanner _scanner =
        new(new FakeFileClassifier());

    [Fact]
    public async Task EmptyDirectory_ReturnsCompletedWithNoFilesAndNoIncidents()
    {
        using var directory = TemporaryDirectory.Create();

        var options = new ScanOptions(directory.Path, [], true);

        var result = await _scanner.ScanAsync(
            options,
            null,
            CancellationToken.None);

        Assert.Equal(ScanStatus.Completed, result.Status);
        Assert.Empty(result.FileEntries);
        Assert.Empty(result.Issues);
        Assert.Empty(result.Classifications);
    }

    [Fact]
    public async Task DirectoryWithFiles_ReturnsFilesAndMetadata()
    {
        using var directory = TemporaryDirectory.Create();

        var firstFile = directory.CreateFile("first.txt", "first file");
        var secondFile = directory.CreateFile("second.txt", "second file");

        var options = new ScanOptions(directory.Path, [], true);

        var result = await _scanner.ScanAsync(
            options,
            null,
            CancellationToken.None);

        Assert.Equal(ScanStatus.Completed, result.Status);
        Assert.Empty(result.Issues);

        var files = result.FileEntries.ToDictionary(file => file.FilePath);

        Assert.Equal(2, files.Count);
        Assert.True(files.ContainsKey(firstFile.FullName));
        Assert.True(files.ContainsKey(secondFile.FullName));

        Assert.Equal(firstFile.Length, files[firstFile.FullName].Size);
        Assert.Equal(secondFile.Length, files[secondFile.FullName].Size);

        Assert.Equal(
            firstFile.LastWriteTimeUtc,
            files[firstFile.FullName].LastModified);

        Assert.Equal(
            secondFile.LastWriteTimeUtc,
            files[secondFile.FullName].LastModified);

        // Verificar las clasificaciones.
        Assert.Equal(2, result.Classifications.Count);

        Assert.All(result.Classifications, classification =>
        {
            Assert.Equal(
                ClassificationCategory.Other,
                classification.Category);

            Assert.Equal(
                ClassificationStatus.Classified,
                classification.Status);

            Assert.Contains(
                ClassificationReason.NoMatchingRule,
                classification.Reason);
        });
    }

    [Fact]
    public async Task AlreadyCancelledToken_ReturnsCancelledWithoutStartingTraversal()
    {
        using var directory = TemporaryDirectory.Create();

        directory.CreateFile("file.txt", "content");

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        var options = new ScanOptions(directory.Path, [], true);

        var result = await _scanner.ScanAsync(
            options,
            null,
            cancellationTokenSource.Token);

        Assert.Equal(ScanStatus.Cancelled, result.Status);
        Assert.Empty(result.FileEntries);
        Assert.Empty(result.Issues);
        Assert.Empty(result.Classifications);
    }

    [Fact]
    public async Task AccessError_RecordsIncidentAndContinuesWhenPossible()
    {
        using var directory = TemporaryDirectory.Create();

        var accessibleDirectory = directory.CreateDirectory("accessible");
        var inaccessibleDirectory = directory.CreateDirectory("inaccessible");

        var accessibleFilePath = Path.Combine(
            accessibleDirectory.FullName,
            "available.txt");

        File.WriteAllText(accessibleFilePath, "content");

        var accessibleFile = new FileInfo(accessibleFilePath);

        DirectoryAccessSnapshot? accessSnapshot = null;

        try
        {
            accessSnapshot = RemoveReadAccess(inaccessibleDirectory);

            var options = new ScanOptions(directory.Path, [], true);

            var result = await _scanner.ScanAsync(
                options,
                null,
                CancellationToken.None);

            Assert.Equal(ScanStatus.Completed, result.Status);

            Assert.Contains(
                result.FileEntries,
                file => file.FilePath == accessibleFile.FullName);

            Assert.NotEmpty(result.Issues);

            Assert.Contains(
                result.Issues,
                incident =>
                    incident.DirectoryPath == inaccessibleDirectory.FullName);

            // El archivo accesible debe tener su clasificación.
            Assert.Contains(
                result.Classifications,
                classification =>
                    classification.FileEntryId ==
                    result.FileEntries.Single(
                        file => file.FilePath == accessibleFile.FullName).Id);
        }
        finally
        {
            if (accessSnapshot is DirectoryAccessSnapshot snapshot)
            {
                RestoreAccess(inaccessibleDirectory, snapshot);
            }
        }
    }

    [Fact]
    public async Task CancellationDuringScan_ReturnsCancelledWithProcessedFiles()
    {
        using var directory = TemporaryDirectory.Create();

        var firstDirectory = directory.CreateDirectory("first");
        var secondDirectory = directory.CreateDirectory("second");

        File.WriteAllText(
            Path.Combine(firstDirectory.FullName, "first.txt"),
            "content");

        File.WriteAllText(
            Path.Combine(secondDirectory.FullName, "second.txt"),
            "content");

        using var cancellationTokenSource = new CancellationTokenSource();

        var options = new ScanOptions(directory.Path, [], true);

        IProgress<ScanProgress> progress =
            new CancellingProgress(cancellationTokenSource);

        var result = await _scanner.ScanAsync(
            options,
            progress,
            cancellationTokenSource.Token);

        Assert.Equal(ScanStatus.Cancelled, result.Status);
        Assert.Single(result.FileEntries);
        Assert.Empty(result.Issues);
        Assert.Single(result.Classifications);

        Assert.Equal(
            result.FileEntries.Single().Id,
            result.Classifications.Single().FileEntryId);
    }

    [Fact]
    public async Task ScanAsync_WhenHiddenFilesAreExcluded_ShouldIgnoreThem()
    {
        string root = CreateTempDirectory();

        try
        {
            string visibleFile = Path.Combine(root, "visible.txt");
            string hiddenFile = Path.Combine(root, "hidden.txt");

            File.WriteAllText(visibleFile, "visible");
            File.WriteAllText(hiddenFile, "hidden");

            File.SetAttributes(
                hiddenFile,
                File.GetAttributes(hiddenFile) | FileAttributes.Hidden);

            ScanOptions options = new(root, [], false);

            ScanResult result = await _scanner.ScanAsync(
                options,
                null,
                CancellationToken.None);

            Assert.Equal(ScanStatus.Completed, result.Status);

            Assert.Contains(
                result.FileEntries,
                file => file.FilePath == visibleFile);

            Assert.DoesNotContain(
                result.FileEntries,
                file => file.FilePath == hiddenFile);

            Assert.Equal(
                result.FileEntries.Count,
                result.Classifications.Count);
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    [Fact]
    public async Task ScanAsync_WhenHiddenDirectoriesAreExcluded_ShouldIgnoreTheirContents()
    {
        string root = CreateTempDirectory();

        try
        {
            string visibleFile = Path.Combine(root, "visible.txt");
            string hiddenDirectory = Path.Combine(root, "HiddenFolder");
            string hiddenDirectoryFile = Path.Combine(
                hiddenDirectory,
                "inside.txt");

            Directory.CreateDirectory(hiddenDirectory);

            File.WriteAllText(visibleFile, "visible");
            File.WriteAllText(hiddenDirectoryFile, "inside");

            DirectoryInfo directoryInfo = new(hiddenDirectory);
            directoryInfo.Attributes |= FileAttributes.Hidden;

            ScanOptions options = new(root, [], false);

            ScanResult result = await _scanner.ScanAsync(
                options,
                null,
                CancellationToken.None);

            Assert.Equal(ScanStatus.Completed, result.Status);

            Assert.Contains(
                result.FileEntries,
                file => file.FilePath == visibleFile);

            Assert.DoesNotContain(
                result.FileEntries,
                file => file.FilePath == hiddenDirectoryFile);

            Assert.Equal(
                result.FileEntries.Count,
                result.Classifications.Count);
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    [Fact]
    public async Task ScanAsync_WhenHiddenFilesAreIncluded_ShouldIncludeHiddenFilesAndDirectories()
    {
        string root = CreateTempDirectory();

        try
        {
            string hiddenFile = Path.Combine(root, "hidden.txt");
            string hiddenDirectory = Path.Combine(root, "HiddenFolder");
            string nestedFile = Path.Combine(hiddenDirectory, "inside.txt");

            File.WriteAllText(hiddenFile, "hidden");

            Directory.CreateDirectory(hiddenDirectory);

            File.WriteAllText(nestedFile, "inside");

            File.SetAttributes(
                hiddenFile,
                File.GetAttributes(hiddenFile) | FileAttributes.Hidden);

            DirectoryInfo directoryInfo = new(hiddenDirectory);
            directoryInfo.Attributes |= FileAttributes.Hidden;

            ScanOptions options = new(root, [], true);

            ScanResult result = await _scanner.ScanAsync(
                options,
                null,
                CancellationToken.None);

            Assert.Equal(ScanStatus.Completed, result.Status);

            Assert.Contains(
                result.FileEntries,
                file => file.FilePath == hiddenFile);

            Assert.Contains(
                result.FileEntries,
                file => file.FilePath == nestedFile);

            Assert.Equal(
                result.FileEntries.Count,
                result.Classifications.Count);
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    [Fact]
    public async Task ScanAsync_WhenCancelledDuringScan_ShouldReturnPartialResults()
    {
        string root = CreateTempDirectory();

        try
        {
            File.WriteAllText(
                Path.Combine(root, "first.txt"),
                "first");

            File.WriteAllText(
                Path.Combine(root, "second.txt"),
                "second");

            using CancellationTokenSource cts = new();

            var progress = new SynchronousProgress<ScanProgress>(p =>
            {
                if (p is FileProcessedProgress)
                {
                    cts.Cancel();
                }
            });

            ScanOptions options = new(root, [], true);

            ScanResult result = await _scanner.ScanAsync(
                options,
                progress,
                cts.Token);

            Assert.Equal(ScanStatus.Cancelled, result.Status);

            Assert.NotEmpty(result.FileEntries);
            Assert.True(result.FileEntries.Count < 2);

            Assert.Equal(
                result.FileEntries.Count,
                result.Classifications.Count);

            Assert.All(
                result.FileEntries,
                file => Assert.Contains(
                    result.Classifications,
                    classification => classification.FileEntryId == file.Id));
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    [Fact]
    public async Task ScanAsync_WhenDirectoryIsExcluded_ShouldNotScanIt()
    {
        string root = CreateTempDirectory();

        try
        {
            string excludedDirectory = Path.Combine(root, "Temp");

            Directory.CreateDirectory(excludedDirectory);

            string visibleFile = Path.Combine(root, "visible.txt");
            string excludedFile = Path.Combine(excludedDirectory, "excluded.txt");

            File.WriteAllText(visibleFile, "visible");
            File.WriteAllText(excludedFile, "excluded");

            ScanOptions options = new(
                root,
                [excludedDirectory],
                true);

            ScanResult result = await _scanner.ScanAsync(
                options,
                null,
                CancellationToken.None);

            Assert.Equal(ScanStatus.Completed, result.Status);

            Assert.Contains(
                result.FileEntries,
                file => file.FilePath == visibleFile);

            Assert.DoesNotContain(
                result.FileEntries,
                file => file.FilePath == excludedFile);

            Assert.Equal(
                result.FileEntries.Count,
                result.Classifications.Count);
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    [Fact]
    public async Task ScanAsync_WhenParentDirectoryIsExcluded_ShouldNotScanItsSubdirectories()
    {
        string root = CreateTempDirectory();

        try
        {
            string excludedDirectory = Path.Combine(root, "Temp");
            string nestedDirectory = Path.Combine(excludedDirectory, "Nested");

            Directory.CreateDirectory(nestedDirectory);

            string nestedFile = Path.Combine(nestedDirectory, "inside.txt");

            File.WriteAllText(nestedFile, "inside");

            ScanOptions options = new(
                root,
                [excludedDirectory],
                true);

            ScanResult result = await _scanner.ScanAsync(
                options,
                null,
                CancellationToken.None);

            Assert.Equal(ScanStatus.Completed, result.Status);

            Assert.DoesNotContain(
                result.FileEntries,
                file => file.FilePath == nestedFile);

            Assert.Empty(result.Classifications);
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    [Fact]
    public async Task ScanAsync_WhenDirectoryNameStartsWithExcludedName_ShouldStillScanIt()
    {
        string root = CreateTempDirectory();

        try
        {
            string excludedDirectory = Path.Combine(root, "Temp");
            string similarDirectory = Path.Combine(root, "Temporary");

            Directory.CreateDirectory(excludedDirectory);
            Directory.CreateDirectory(similarDirectory);

            string excludedFile = Path.Combine(excludedDirectory, "excluded.txt");
            string expectedFile = Path.Combine(similarDirectory, "expected.txt");

            File.WriteAllText(excludedFile, "excluded");
            File.WriteAllText(expectedFile, "expected");

            ScanOptions options = new(
                root,
                [excludedDirectory],
                true);

            ScanResult result = await _scanner.ScanAsync(
                options,
                null,
                CancellationToken.None);

            Assert.Equal(ScanStatus.Completed, result.Status);

            Assert.DoesNotContain(
                result.FileEntries,
                file => file.FilePath == excludedFile);

            Assert.Contains(
                result.FileEntries,
                file => file.FilePath == expectedFile);

            Assert.Single(result.Classifications);
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    private sealed class FakeFileClassifier : IFileClassifier
    {
        public ClassificationResult ClassifyFile(FileEntry file)
        {
            return new ClassificationResult(
                file.Id,
                ClassificationCategory.Other,
                new[]
                {
                    ClassificationReason.NoMatchingRule
                },
                ClassificationStatus.Classified);
        }
    }

    private sealed class CancellingProgress : IProgress<ScanProgress>
    {
        private readonly CancellationTokenSource _cancellationTokenSource;

        public CancellingProgress(
            CancellationTokenSource cancellationTokenSource)
        {
            _cancellationTokenSource = cancellationTokenSource;
        }

        public void Report(ScanProgress value)
        {
            if (value is FileProcessedProgress)
            {
                _cancellationTokenSource.Cancel();
            }
        }
    }

    private readonly record struct DirectoryAccessSnapshot(
        UnixFileMode? UnixFileMode);

    private static DirectoryAccessSnapshot RemoveReadAccess(
    DirectoryInfo directory)
    {
        if (OperatingSystem.IsWindows())
        {
            var security = directory.GetAccessControl();

            var denyRule = new FileSystemAccessRule(
                WindowsIdentity.GetCurrent().User!,
                FileSystemRights.ListDirectory,
                InheritanceFlags.None,
                PropagationFlags.None,
                AccessControlType.Deny);

            security.AddAccessRule(denyRule);

            directory.SetAccessControl(security);

            return new DirectoryAccessSnapshot(null);
        }

        var originalMode = File.GetUnixFileMode(directory.FullName);

        File.SetUnixFileMode(
            directory.FullName,
            UnixFileMode.UserWrite |
            UnixFileMode.UserExecute);

        return new DirectoryAccessSnapshot(originalMode);
    }

    private static void RestoreAccess(
    DirectoryInfo directory,
    DirectoryAccessSnapshot snapshot)
    {
        if (OperatingSystem.IsWindows())
        {
            var security = directory.GetAccessControl();

            var denyRule = new FileSystemAccessRule(
                WindowsIdentity.GetCurrent().User!,
                FileSystemRights.ListDirectory,
                InheritanceFlags.None,
                PropagationFlags.None,
                AccessControlType.Deny);

            security.RemoveAccessRuleSpecific(denyRule);

            directory.SetAccessControl(security);

            return;
        }

        if (snapshot.UnixFileMode.HasValue)
        {
            File.SetUnixFileMode(
                directory.FullName,
                snapshot.UnixFileMode.Value);
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private TemporaryDirectory(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TemporaryDirectory Create()
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"CleanStats-{Guid.NewGuid():N}");

            Directory.CreateDirectory(path);

            return new TemporaryDirectory(path);
        }

        public FileInfo CreateFile(string name, string content)
        {
            var path = System.IO.Path.Combine(Path, name);

            File.WriteAllText(path, content);

            return new FileInfo(path);
        }

        public DirectoryInfo CreateDirectory(string name)
        {
            return Directory.CreateDirectory(
                System.IO.Path.Combine(Path, name));
        }

        public void Dispose()
        {
            Directory.Delete(Path, recursive: true);
        }
    }
    private sealed class ThrowingFileClassifier : IFileClassifier
    {
        public ClassificationResult ClassifyFile(FileEntry file)
        {
            throw new InvalidOperationException(
                "Error simulado durante la clasificación.");
        }
    }

    private static string CreateTempDirectory()
    {
        string path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"CleanStatsTests_{Guid.NewGuid():N}");

        Directory.CreateDirectory(path);

        return path;
    }

    private static void DeleteTempDirectory(string path)
    {
        if (!Directory.Exists(path))
            return;

        foreach (string file in Directory.EnumerateFiles(
                     path,
                     "*",
                     SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        foreach (string directory in Directory.EnumerateDirectories(
                     path,
                     "*",
                     SearchOption.AllDirectories))
        {
            File.SetAttributes(directory, FileAttributes.Directory);
        }

        File.SetAttributes(path, FileAttributes.Directory);

        Directory.Delete(path, recursive: true);
    }
    [Fact]
    public async Task ScanAsync_WhenClassificationFails_ShouldNotIncludeFileAsProcessed()
    {
        string root = CreateTempDirectory();

        try
        {
            string filePath = Path.Combine(root, "test.txt");
            File.WriteAllText(filePath, "content");

            var scanner = new FileScanner(new ThrowingFileClassifier());

            var result = await scanner.ScanAsync(
                new ScanOptions(root, [], true),
                null,
                CancellationToken.None);

            Assert.Empty(result.FileEntries);
            Assert.Empty(result.Classifications);

            Assert.Single(result.Issues);
            Assert.Equal(ScanIssueError.FileError, result.Issues[0].ErrorType);
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }
    [Fact]
    public async Task ScanAsync_WhenClassificationFails_ShouldReportFileFailure()
    {
        string root = CreateTempDirectory();

        try
        {
            string filePath = Path.Combine(root, "test.txt");
            File.WriteAllText(filePath, "content");

            var scanner = new FileScanner(new ThrowingFileClassifier());

            var progressEvents = new List<ScanProgress>();

            var progress = new SynchronousProgress<ScanProgress>(
                progressEvents.Add);

            await scanner.ScanAsync(
                new ScanOptions(root, [], true),
                progress,
                CancellationToken.None);

            Assert.Contains(
                progressEvents,
                item => item is FileFailedProgress);
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }
}
