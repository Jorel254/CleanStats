using CleanStats.Core.Contracts;
using CleanStats.Core.Model;
using CleanStats.Core.Progress;

namespace CleanStats.Infrastructure
{
    public class FileScanner : IFileScanner
    {
        public Task<ScanResult> ScanAsync(ScanOptions options, IProgress<ScanProgress>? progress, CancellationToken cancellationToken)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            Queue<string> directoriesToScan = new Queue<string>();
            directoriesToScan.Enqueue(options.RootPath);
            List<FileEntry> fileEntries = new List<FileEntry>();
            List<ScanIssue> issues = new List<ScanIssue>();
            throw new NotImplementedException();
        }
    }
}
