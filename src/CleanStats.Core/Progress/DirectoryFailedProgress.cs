using System;
using System.Collections.Generic;
using System.Text;
using CleanStats.Core.Model;
namespace CleanStats.Core.Progress;

public class DirectoryFailedProgress : ScanProgress
{
    public ScanIssue Issue { get; }

    public DirectoryFailedProgress(ScanIssue issue)
    {
        if (issue == null)
        {
            throw new ArgumentNullException(nameof(issue));
        }
        if (issue.ErrorType == ScanIssueError.DirectoryError && !string.IsNullOrWhiteSpace(issue.DirectoryPath))
        {
            Issue = issue;
        }
        else
        {
            throw new ArgumentException("El issue debe tener una ruta de directorio válida para el tipo DirectoryError.", nameof(issue));
        }

    }
}
