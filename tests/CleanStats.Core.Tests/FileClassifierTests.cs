
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CleanStats.Core;
using CleanStats.Core.Model;

using Xunit;

namespace CleanStats.Tests;

public class FileClassifierTests
{
    private readonly FileClassifier _classifier = new();

    private static FileEntry CreateFile(
        string path,
        DateTime? lastModified = null)
    {
        return new FileEntry(
            path,
            1024,
            lastModified,
            DateTime.UtcNow.AddYears(-1)
        );
    }

    [Theory]
    [InlineData("archivo.tmp")]
    [InlineData("archivo.temp")]
    [InlineData("archivo.bak")]
    [InlineData("archivo.old")]
    [InlineData("archivo.part")]
    [InlineData("archivo.TMP")]
    public void DetectionFile_TemporaryExtension_AddsTemporaryReason(
        string fileName)
    {
        var file = CreateFile(
            Path.Combine("C:\\Pruebas", fileName),
            DateTime.UtcNow);

        var result = _classifier.DetectionFile(file);

        Assert.Contains(
            ClassificationReason.ExtensionMatchesTemporary,
            result.Reasons);
    }

    [Theory]
    [InlineData("archivo.log")]
    [InlineData("archivo.etl")]
    [InlineData("archivo.LOG")]
    public void DetectionFile_LogExtension_AddsLogReason(
        string fileName)
    {
        var file = CreateFile(
            Path.Combine("C:\\Pruebas", fileName),
            DateTime.UtcNow);

        var result = _classifier.DetectionFile(file);

        Assert.Contains(
            ClassificationReason.ExtensionMatchesLog,
            result.Reasons);
    }

    [Theory]
    [InlineData("archivo.dmp")]
    [InlineData("archivo.mdmp")]
    [InlineData("archivo.hdmp")]
    [InlineData("archivo.DMP")]
    public void DetectionFile_DumpExtension_AddsDumpReason(
        string fileName)
    {
        var file = CreateFile(
            Path.Combine("C:\\Pruebas", fileName),
            DateTime.UtcNow);

        var result = _classifier.DetectionFile(file);

        Assert.Contains(
            ClassificationReason.MatchesKnownDumpExtension,
            result.Reasons);
    }

    [Theory]
    [InlineData("C:\\Pruebas\\Cache\\archivo.dat")]
    [InlineData("C:\\Pruebas\\Caches\\archivo.dat")]
    [InlineData("C:\\Pruebas\\Code Cache\\archivo.dat")]
    [InlineData("C:\\Pruebas\\GPUCache\\archivo.dat")]
    [InlineData("C:\\Pruebas\\ShaderCache\\archivo.dat")]
    [InlineData("C:\\Pruebas\\INetCache\\archivo.dat")]
    [InlineData("C:\\Pruebas\\cache\\archivo.dat")]
    public void DetectionFile_CacheDirectory_AddsCacheReason(
        string path)
    {
        var file = CreateFile(path, DateTime.UtcNow);

        var result = _classifier.DetectionFile(file);

        Assert.Contains(
            ClassificationReason.PathMatchesCacheDirectory,
            result.Reasons);
    }

    [Fact]
    public void DetectionFile_CacheBackupDirectory_DoesNotAddCacheReason()
    {
        var file = CreateFile(
            "C:\\Pruebas\\CacheBackup\\archivo.dat",
            DateTime.UtcNow);

        var result = _classifier.DetectionFile(file);

        Assert.DoesNotContain(
            ClassificationReason.PathMatchesCacheDirectory,
            result.Reasons);
    }

    [Fact]
    public void DetectionFile_FileOlderThan90Days_AddsAgeReason()
    {
        var file = CreateFile(
            "C:\\Pruebas\\archivo.dat",
            DateTime.UtcNow.AddDays(-91));

        var result = _classifier.DetectionFile(file);

        Assert.Contains(
            ClassificationReason.ExceedsAgeThreshold,
            result.Reasons);
    }

    [Fact]
    public void DetectionFile_RecentFile_DoesNotAddAgeReason()
    {
        var file = CreateFile(
            "C:\\Pruebas\\archivo.dat",
            DateTime.UtcNow.AddDays(-10));

        var result = _classifier.DetectionFile(file);

        Assert.DoesNotContain(
            ClassificationReason.ExceedsAgeThreshold,
            result.Reasons);
    }

    [Fact]
    public void DetectionFile_MissingModificationDate_AddsManualReviewReason()
    {
        var file = CreateFile("C:\\Pruebas\\archivo.dat");

        var result = _classifier.DetectionFile(file);

        Assert.False(result.HasLastModificationDate);
        Assert.Contains(
            ClassificationReason.RequiresManualReview,
            result.Reasons);
    }

    [Fact]
    public void DetectionFile_NoMatchingRules_AddsNoMatchingRuleReason()
    {
        var file = CreateFile(
            "C:\\Pruebas\\archivo.dat",
            DateTime.UtcNow);

        var result = _classifier.DetectionFile(file);

        Assert.Contains(
            ClassificationReason.NoMatchingRule,
            result.Reasons);
    }

    [Fact]
    public void DetectionFile_MultipleRules_AddsAllMatchingReasons()
    {
        var file = CreateFile(
            "C:\\Pruebas\\Cache\\archivo.log",
            DateTime.UtcNow.AddDays(-100));

        var result = _classifier.DetectionFile(file);

        Assert.Contains(
            ClassificationReason.ExtensionMatchesLog,
            result.Reasons);

        Assert.Contains(
            ClassificationReason.PathMatchesCacheDirectory,
            result.Reasons);

        Assert.Contains(
            ClassificationReason.ExceedsAgeThreshold,
            result.Reasons);

        Assert.DoesNotContain(
            ClassificationReason.NoMatchingRule,
            result.Reasons);
    }

    [Fact]
    public void DetectionFile_NullFileEntry_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => _classifier.DetectionFile(null!));
    }

    [Fact]
    public void DetectionFile_ResultContainsUniqueReasons()
    {
        var file = CreateFile(
            "C:\\Pruebas\\Cache\\archivo.log",
            DateTime.UtcNow.AddDays(-100));

        var result = _classifier.DetectionFile(file);

        Assert.Equal(
            result.Reasons.Count,
            result.Reasons.Distinct().Count());
    }

    [Fact]
    public void ClassifyFile_NullFileEntry_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => _classifier.ClassifyFile(null!)
        );
    }

    [Theory]
    [InlineData("document.tmp", ClassificationCategory.Temporary)]
    [InlineData("document.temp", ClassificationCategory.Temporary)]
    [InlineData("backup.bak", ClassificationCategory.Temporary)]
    [InlineData("application.log", ClassificationCategory.Log)]
    [InlineData("system.etl", ClassificationCategory.Log)]
    [InlineData("memory.dmp", ClassificationCategory.Dump)]
    [InlineData("memory.mdmp", ClassificationCategory.Dump)]
    [InlineData("memory.hdmp", ClassificationCategory.Dump)]
    [InlineData("document.pdf", ClassificationCategory.Other)]
    public void ClassifyFile_KnownExtensions_ReturnsExpectedCategory(
        string fileName,
        ClassificationCategory expectedCategory)
    {
        var file = CreateFile(fileName);

        var result = _classifier.ClassifyFile(file);

        Assert.Equal(expectedCategory, result.Category);
    }

    [Theory]
    [InlineData(@"C:\Users\Test\AppData\Local\Cache\document.dat")]
    [InlineData(@"C:\Users\Test\AppData\Local\Caches\document.dat")]
    [InlineData(@"C:\Users\Test\AppData\Local\Code Cache\document.dat")]
    [InlineData(@"C:\Users\Test\AppData\Local\GPUCache\document.dat")]
    [InlineData(@"C:\Users\Test\AppData\Local\ShaderCache\document.dat")]
    [InlineData(@"C:\Users\Test\AppData\Local\INetCache\document.dat")]
    public void ClassifyFile_CacheDirectory_ReturnsCacheCategory(
        string filePath)
    {
        var file = CreateFile(filePath);

        var result = _classifier.ClassifyFile(file);

        Assert.Equal(ClassificationCategory.Cache, result.Category);
        Assert.Contains(
            ClassificationReason.PathMatchesCacheDirectory,
            result.Reason
        );
    }

    [Fact]
    public void ClassifyFile_DumpAndTemporaryExtension_ReturnsDumpCategory()
    {
        var file = CreateFile("backup.tmp.dmp");

        var result = _classifier.ClassifyFile(file);

        Assert.Equal(ClassificationCategory.Dump, result.Category);
    }

    [Fact]
    public void ClassifyFile_TemporaryExtensionAndCachePath_ReturnsTemporaryCategory()
    {
        var file = CreateFile(
            @"C:\Users\Test\Cache\backup.tmp"
        );

        var result = _classifier.ClassifyFile(file);

        Assert.Equal(ClassificationCategory.Temporary, result.Category);
    }

    [Fact]
    public void ClassifyFile_LogExtensionAndCachePath_ReturnsCacheCategory()
    {
        var file = CreateFile(
            @"C:\Users\Test\Cache\application.log"
        );

        var result = _classifier.ClassifyFile(file);

        Assert.Equal(ClassificationCategory.Cache, result.Category);
    }

    [Fact]
    public void ClassifyFile_MissingLastModifiedDate_ReturnsPendingReview()
    {
        var file = CreateFile(
            "document.tmp",
            lastModified: null
        );

        var result = _classifier.ClassifyFile(file);

        Assert.Equal(ClassificationStatus.PendingReview, result.Status);
        Assert.Contains(
            ClassificationReason.RequiresManualReview,
            result.Reason
        );
    }

    [Fact]
    public void ClassifyFile_OldFileWithLastModifiedDate_ReturnsClassified()
    {
        var file = CreateFile(
            "document.tmp",
            lastModified: DateTime.UtcNow.AddDays(-120)
        );

        var result = _classifier.ClassifyFile(file);

        Assert.Equal(ClassificationStatus.Classified, result.Status);
        Assert.Contains(
            ClassificationReason.ExceedsAgeThreshold,
            result.Reason
        );
    }

    [Fact]
    public void ClassifyFile_RecentFile_ReturnsClassified()
    {
        var file = CreateFile(
            "document.tmp",
            lastModified: DateTime.UtcNow.AddDays(-10)
        );

        var result = _classifier.ClassifyFile(file);

        Assert.Equal(ClassificationStatus.Classified, result.Status);
        Assert.DoesNotContain(
            ClassificationReason.ExceedsAgeThreshold,
            result.Reason
        );
    }

    [Fact]
    public void ClassifyFile_NoMatchingRule_ReturnsOtherCategory()
    {
        // Arrange
        var file = CreateFile(
            "document.pdf",
            lastModified: DateTime.UtcNow.AddDays(-10)
        );

        // Act
        var result = _classifier.ClassifyFile(file);

        // Assert
        Assert.Equal(ClassificationCategory.Other, result.Category);
        Assert.Equal(ClassificationStatus.Classified, result.Status);

        Assert.Contains(
            ClassificationReason.NoMatchingRule,
            result.Reason
        );

        Assert.DoesNotContain(
            ClassificationReason.RequiresManualReview,
            result.Reason
        );
    }

    [Fact]
    public void ClassifyFile_ResultPreservesFileEntryId()
    {
        var file = CreateFile("document.tmp");

        var result = _classifier.ClassifyFile(file);

        Assert.Equal(file.Id, result.FileEntryId);
    }

    [Fact]
    public void ClassifyFile_ResultHasClassificationDateInUtc()
    {
        var file = CreateFile("document.tmp");

        var result = _classifier.ClassifyFile(file);

        Assert.Equal(DateTimeKind.Utc, result.ClassifiedAt.Kind);
    }

    [Fact]
    public void ClassifyFile_ResultContainsMultipleReasons()
    {
        var file = CreateFile(
            @"C:\Users\Test\Cache\document.tmp",
            lastModified: DateTime.UtcNow.AddDays(-120)
        );

        var result = _classifier.ClassifyFile(file);

        Assert.Contains(
            ClassificationReason.ExtensionMatchesTemporary,
            result.Reason
        );
        Assert.Contains(
            ClassificationReason.PathMatchesCacheDirectory,
            result.Reason
        );
        Assert.Contains(
            ClassificationReason.ExceedsAgeThreshold,
            result.Reason
        );

        Assert.Equal(ClassificationCategory.Temporary, result.Category);
        Assert.Equal(ClassificationStatus.Classified, result.Status);
    }
}
