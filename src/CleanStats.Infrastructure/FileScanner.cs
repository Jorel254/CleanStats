using CleanStats.Core.Contracts;
using CleanStats.Core.Model;
using CleanStats.Core.Progress;
using System.IO;
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
            if (cancellationToken.IsCancellationRequested)
            {
                ScanResult result = new ScanResult([], [], ScanStatus.Cancelled);
                return Task.FromResult(result);
            }

            return Task.Run(() => ScanCore(options, progress, cancellationToken));
        }
        private ScanResult ScanCore(ScanOptions options, IProgress<ScanProgress>? progress, CancellationToken cancellationToken)
        {
            Queue<string> directoriesToScan = new Queue<string>();
            directoriesToScan.Enqueue(options.RootPath);
            List<FileEntry> fileEntries = new List<FileEntry>();
            List<ScanIssue> issues = new List<ScanIssue>();
            while (directoriesToScan.Count > 0)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return new ScanResult(fileEntries.ToArray(), issues.ToArray(), ScanStatus.Cancelled);
                }
                string nextDirectory = directoriesToScan.Dequeue();
                try
                {
                    foreach (string subDirectory in Directory.EnumerateDirectories(nextDirectory))
                    {
                        if (!options.IncludeHiddenFiles)
                        {
                            DirectoryInfo dirInfo = new DirectoryInfo(subDirectory);
                            if (dirInfo.Attributes.HasFlag(FileAttributes.Hidden))
                            {
                                continue;
                            }
                        }
                        if (IsExcludedPath(subDirectory, options.ExcludedPaths))
                        {
                            continue;
                        }
                        directoriesToScan.Enqueue(subDirectory);
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    ScanIssue issue = new ScanIssue(
                        null,
                        nextDirectory,
                        ScanIssueError.DirectoryError,
                        $"No se pudieron enumerar los subdirectorios de: {nextDirectory}",
                        ex.ToString());
                    issues.Add(issue);
                    progress?.Report(new DirectoryFailedProgress(issue));
                }
                catch (IOException ex)
                {
                    ScanIssue issue = new ScanIssue(
                        null,
                        nextDirectory,
                        ScanIssueError.DirectoryError,
                        $"No se pudieron enumerar los subdirectorios: {nextDirectory}",
                        ex.ToString());
                    issues.Add(issue);
                    progress?.Report(new DirectoryFailedProgress(issue));
                }
                try
                {
                    foreach (string filePath in Directory.EnumerateFiles(nextDirectory))
                    {
                        // Por ahora, solo identificar el archivo.
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return new ScanResult(
                                fileEntries.ToArray(),
                                issues.ToArray(),
                                ScanStatus.Cancelled);
                        }
                        progress?.Report(new FileDiscoveredProgress(filePath));
                        try
                        {
                            FileInfo fileInfo = new FileInfo(filePath);
                            if (!options.IncludeHiddenFiles)
                            {
                                if (fileInfo.Attributes.HasFlag(FileAttributes.Hidden))
                                {
                                    continue;
                                }
                            }
                            FileEntry entry = new FileEntry(filePath, fileInfo.Length, fileInfo.LastWriteTimeUtc, fileInfo.CreationTimeUtc);
                            fileEntries.Add(entry);
                            progress?.Report(new FileProcessedProgress(entry));
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            ScanIssue issue = new ScanIssue(
                                filePath,
                                null,
                                ScanIssueError.FileError,
                                $"No se pudieron obtener los datos del archivo: {filePath}",
                                ex.ToString());
                            issues.Add(issue);
                            progress?.Report(new FileFailedProgress(issue));
                        }
                        catch (IOException ex)
                        {
                            ScanIssue issue = new ScanIssue(
                                filePath,
                                null,
                                ScanIssueError.FileError,
                                $"No se pudieron obtener los datos del archivo: {filePath}",
                                ex.ToString());
                            issues.Add(issue);
                            progress?.Report(new FileFailedProgress(issue));
                        }
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    ScanIssue issue = new ScanIssue(
                        null,
                        nextDirectory,
                        ScanIssueError.DirectoryError,
                        $"No se pudieron enumerar los archivos de: {nextDirectory}",
                        ex.ToString());
                    issues.Add(issue);
                    progress?.Report(new DirectoryFailedProgress(issue));
                }
                catch (IOException ex)
                {
                    ScanIssue issue = new ScanIssue(
                        null,
                        nextDirectory,
                        ScanIssueError.DirectoryError,
                        $"No se pudieron enumerar los archivos: {nextDirectory}",
                        ex.ToString());
                    issues.Add(issue);
                    progress?.Report(new DirectoryFailedProgress(issue));
                }
            }
            return new ScanResult(fileEntries.ToArray(), issues.ToArray(), ScanStatus.Completed);
        }
        private bool IsExcludedPath(string path, string[] excludePaths)
        {
            if (excludePaths == null || excludePaths.Length == 0)
                return false;

            string fullPath = Path.GetFullPath(path);

            foreach (string excludedPath in excludePaths)
            {
                string normalizedExcluded = Path.TrimEndingDirectorySeparator(
                    Path.GetFullPath(excludedPath));

                // Caso 1: La ruta es exactamente igual a la excluida.
                if (string.Equals(
                    fullPath,
                    normalizedExcluded,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // Caso 2: La ruta está dentro del directorio excluido.
                string excludedPrefix = Path.EndsInDirectorySeparator(normalizedExcluded)
                    ? normalizedExcluded
                    : normalizedExcluded + Path.DirectorySeparatorChar;

                if (fullPath.StartsWith(
                    excludedPrefix,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
