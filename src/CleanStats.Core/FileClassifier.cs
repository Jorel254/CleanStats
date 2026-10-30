using CleanStats.Core.Contracts;
using CleanStats.Core.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace CleanStats.Core;

public class FileDetectionResult
{
    public IReadOnlyList<ClassificationReason> Reasons { get; }
    public bool HasLastModificationDate { get; }

    public FileDetectionResult(
        IEnumerable<ClassificationReason> reasons,
        bool hasLastModificationDate)
    {
        if (reasons == null)
            throw new ArgumentNullException(nameof(reasons));

        Reasons = Array.AsReadOnly(reasons.ToArray());
        HasLastModificationDate = hasLastModificationDate;
    }
}
public class FileClassifier : IFileClassifier
{
    public FileClassifier()
    {
    }

    public FileDetectionResult DetectionFile(FileEntry fileEntry)
    {
        if (fileEntry == null)
        {
            throw new ArgumentNullException(nameof(fileEntry));
        }

        var reasons = new List<ClassificationReason>();
        bool hasLastModificationDate = fileEntry.LastModified != null;
        string[] cachePaths = { "Cache", "Caches", "Code Cache", "GPUCache", "ShaderCache", "INetCache" };
        var extension = validateExtension(fileEntry.FileExtension);
        if (extension != null)
        {
            reasons.Add(extension.Value);
        }

        if (fileEntry.FilePath != null && cachePaths.Any(cachePath => PathContainsDirectoryComponent(fileEntry.FilePath, cachePath)))
        {
            reasons.Add(ClassificationReason.PathMatchesCacheDirectory);
        }
        if (!hasLastModificationDate)
        {
            reasons.Add(ClassificationReason.RequiresManualReview);
        }
        if (hasLastModificationDate && fileEntry.LastModified <= DateTime.UtcNow.AddDays(-90))
        {
            reasons.Add(ClassificationReason.ExceedsAgeThreshold);
        }
        if (reasons.Count == 0)
        {
            reasons.Add(ClassificationReason.NoMatchingRule);
        }
        return new FileDetectionResult(
            reasons.Distinct(),
            hasLastModificationDate
        );
    }
    private static bool PathContainsDirectoryComponent(string filePath, string directoryName)
    {
        string[] components = filePath.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
        return components.Any(component => string.Equals(component, directoryName, StringComparison.OrdinalIgnoreCase));
    }

    private ClassificationReason? validateExtension(string? extension)
    {
        switch (extension?.ToLowerInvariant() ?? string.Empty)
        {
            case ".log":
            case ".etl":
                return ClassificationReason.ExtensionMatchesLog;
            case ".dmp":
            case ".mdmp":
            case ".hdmp":
                return ClassificationReason.MatchesKnownDumpExtension;
            case ".tmp":
            case ".temp":
            case ".bak":
            case ".old":
            case ".part":
                return ClassificationReason.ExtensionMatchesTemporary;
        }
        return null;
    }
    private ClassificationCategory GetClassificationCategory(IEnumerable<ClassificationReason> reason)
    {
        if (reason.Contains(ClassificationReason.MatchesKnownDumpExtension))
        {
            return ClassificationCategory.Dump;
        }
        else if (reason.Contains(ClassificationReason.ExtensionMatchesTemporary))
        {
            return ClassificationCategory.Temporary;
        }
        else if (reason.Contains(ClassificationReason.PathMatchesCacheDirectory))
        {
            return ClassificationCategory.Cache;
        }
        else if (reason.Contains(ClassificationReason.ExtensionMatchesLog))
        {
            return ClassificationCategory.Log;
        }
        else
        {
            return ClassificationCategory.Other;
        }
    }
    private ClassificationStatus GetClassificationStatus(IEnumerable<ClassificationReason> reasons)
    {
        if (reasons.Contains(ClassificationReason.RequiresManualReview))
        {
            return ClassificationStatus.PendingReview;
        }
        else
        {
            return ClassificationStatus.Classified;
        }
    }
    public ClassificationResult ClassifyFile(FileEntry fileEntry)
    {
        if (fileEntry == null)
        {
            throw new ArgumentNullException(nameof(fileEntry));
        }
        var detectionResult = DetectionFile(fileEntry);
        var category = GetClassificationCategory(detectionResult.Reasons);
        var status = GetClassificationStatus(detectionResult.Reasons);
        return new ClassificationResult(fileEntry.Id,
            category,
            detectionResult.Reasons,
            status
        );
    }
}
