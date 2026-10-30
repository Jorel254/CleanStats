using System;
using System.Collections.Generic;
using System.Text;
using CleanStats.Core.Model;

namespace CleanStats.Core.Progress;

public class FileProcessedProgress : ScanProgress
{
    public FileEntry File { get; }
    public ClassificationResult ClassificationResult { get; }
    public FileProcessedProgress(FileEntry file, ClassificationResult classificationResult)
    {
        if (file == null)
        {
            throw new ArgumentNullException(nameof(file));
        }
        if (classificationResult == null)
        {
            throw new ArgumentNullException(nameof(classificationResult));
        }
        File = file;
        ClassificationResult = classificationResult;
    }
}
