using CleanStats.Core.Model;
using CleanStats.Core.Progress;
using System;
using System.Collections.Generic;
using System.Text;

namespace CleanStats.Core.Contracts;

public interface IScanCoordinator
{
    Task<ScanSummary> StartScanAsync(ScanOptions options, CancellationToken cancellationToken);
    ScanSummary? CurrentSummary { get; }
    void CancelScan();
    event EventHandler<NotificationProgress> ProgressChanged;
}
public class NotificationProgress : EventArgs
{
    public ScanProgress Progress { get; }
    public ScanSummary Summary { get; }
    public NotificationProgress(ScanProgress progress, ScanSummary summary)
    {
        if (progress == null) throw new ArgumentNullException(nameof(progress));
        if(summary == null) throw new ArgumentNullException(nameof(summary));
        Progress = progress;
        Summary = summary;
    }
}
