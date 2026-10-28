using System.Threading;
using System.Threading.Tasks;
using CleanStats.Core.Model;
using CleanStats.Core.Progress;
namespace CleanStats.Core.Contracts;

public interface IFileScanner
{
    Task<ScanResult> ScanAsync(ScanOptions options, IProgress<ScanProgress>? progress, CancellationToken cancellationToken);
}
