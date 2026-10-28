using CleanStats.Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace CleanStats.Core.Progress;

public class FileFailedProgress : ScanProgress
{
    public ScanIssue Issue { get; }
    public FileFailedProgress(ScanIssue issue)
    {
        if (issue == null)
        {
            throw new ArgumentNullException(nameof(issue));
        }
        if (issue.ErrorType == ScanIssueError.FileError && !string.IsNullOrWhiteSpace(issue.FilePath))
        {
            Issue = issue;
        }
        else
        {
            throw new ArgumentException("El issue debe tener una ruta de archivo válida para el tipo FileError.", nameof(issue));
        }

    }
}
