using System;
using System.Collections.Generic;
using System.Text;
using CleanStats.Core.Progress;

namespace CleanStats.Core.Progress;

public class FileDiscoveredProgress : ScanProgress
{
    public string FilePath { get; }

    public FileDiscoveredProgress(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));
        }
        FilePath = filePath;
    }
}
