using CleanStats.Core.Contracts;
using CleanStats.Core.Model;
using CleanStats.Core.Progress;
using System;
using System.Collections.Generic;
using System.Text;

namespace CleanStats.Infrastructure;

public class ScanCoordinator(IFileScanner scanner) : IScanCoordinator
{
    private readonly IFileScanner _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
    private ScanSummary? _currentSummary;
    public ScanSummary? CurrentSummary => _currentSummary;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly SemaphoreSlim _scanLock = new SemaphoreSlim(1, 1);
    private readonly object _scanLockObject = new object();
    private bool _isScanActive = false;
    private ScanResult? _currentResult;
    public ScanResult? CurrentResult => _currentResult;

    public event EventHandler<NotificationProgress> ProgressChanged;

    public async Task<ScanSummary> StartScanAsync(ScanOptions options, CancellationToken cancellationToken)
    {
        CancellationTokenSource? cts = null;
        ScanSummary summary = new ScanSummary();
        bool scanStarted = false;
        await _scanLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            try
            {
                if (_isScanActive)
                {
                    throw new InvalidOperationException("A scan is already in progress.");
                }

                if (options is null)
                {
                    throw new ArgumentNullException(nameof(options));
                }

                if (Directory.Exists(options.RootPath) == false)
                {
                    throw new DirectoryNotFoundException($"The specified root path does not exist: {options.RootPath}");
                }
                _isScanActive = true;
                scanStarted = true;
                _currentResult = null;
                _currentSummary = summary;
                summary.StartScan();
                lock (_scanLockObject)
                {
                    cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    _cancellationTokenSource = cts;
                }

            }
            finally
            {
                _scanLock.Release();
            }
            IProgress<ScanProgress> progress = new SynchronousProgress<ScanProgress>(p => HandleProgress(summary, p));
            var result = await _scanner.ScanAsync(options, progress, cts.Token).ConfigureAwait(false);
            summary.UpdateScanStatus(result.Status);
            _currentResult = new ScanResult( result.FileEntries.ToArray(),result.Issues.ToArray(),result.Classifications.ToArray(),result.Status);
            return summary;
        }
        catch (OperationCanceledException)
        {
            if (summary.Status == ScanStatus.InProgress || summary.Status == ScanStatus.NotStarted)
            {
                summary.UpdateScanStatus(ScanStatus.Cancelled);
            }
            return summary;
        }
        catch (Exception)
        {
            if (summary.Status == ScanStatus.InProgress)
            {
                summary.UpdateScanStatus(ScanStatus.Failed);
            }
            throw;
        }
        finally
        {
            await _scanLock.WaitAsync().ConfigureAwait(false);

            try
            {
                if (scanStarted)
                {
                    _isScanActive = false;
                }

                lock (_scanLockObject)
                {
                    if (ReferenceEquals(_cancellationTokenSource, cts))
                    {
                        _cancellationTokenSource = null;
                    }

                    cts?.Dispose();
                }
            }
            finally
            {
                _scanLock.Release();
            }
        }
    }

    public void CancelScan()
    {
        lock (_scanLockObject)
        {
            if (_cancellationTokenSource != null &&
                !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }
        }
    }
    private void HandleProgress(ScanSummary summary, ScanProgress progress)
    {
        switch (progress)
        {
            case FileDiscoveredProgress fileDiscovered:
                summary.UpdateFoundFiles();
                break;
            case FileProcessedProgress fileProcessed:
                summary.UpdateCorrectFile(fileProcessed.File.Size);
                break;
            case FileFailedProgress fileFailed:
                summary.UpdateFailedFile(fileFailed.Issue);
                break;
            case DirectoryFailedProgress directoryFailed:
                summary.AddDirectoryIssue(directoryFailed.Issue);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(progress), "Unknown progress type.");
        }
        ProgressChanged?.Invoke(this, new NotificationProgress(progress, summary));
    }
}
public sealed class SynchronousProgress<T> : IProgress<T>
{
    private readonly Action<T> _handler;

    public SynchronousProgress(Action<T> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public void Report(T value)
    {
        _handler(value);
    }
}
