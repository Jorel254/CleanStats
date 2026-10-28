using System;
using System.Collections.Generic;
using System.Text;
using CleanStats.Core.Model;

namespace CleanStats.Core.Progress;

public class FileProcessedProgress : ScanProgress
{
    public FileEntry File { get; }
    public FileProcessedProgress(FileEntry file)
    {
        if (file == null)
        {
            throw new ArgumentNullException(nameof(file));
        }
        File = file;
    }
}
