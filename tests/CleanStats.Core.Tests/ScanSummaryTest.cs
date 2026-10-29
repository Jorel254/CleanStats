using CleanStats.Core.Model;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit.Sdk;
namespace CleanStats.Core.Tests;

public class ScanSummaryTest
{
    [Fact]
    public void ValidateClass()
    {
        var scanSummary = new ScanSummary();
        Assert.NotNull(scanSummary);
        Assert.Equal(ScanStatus.NotStarted, scanSummary.Status);
        scanSummary.StartScan();
        Assert.Equal(ScanStatus.InProgress, scanSummary.Status);
        Assert.True(scanSummary.StartTime > DateTime.MinValue);
        //scanSummary.StartScan();
        //scanSummary.UpdateScanStatus(ScanStatus.Cancelled);
        //scanSummary.UpdateFoundFiles();
        //scanSummary.UpdateCorrectFile(100);
        //scanSummary.UpdateFailedFile(new ScanIssue("C:\\path\\to\\file", ScanIssueError.FileError, "Error message", "Technical error message"));
        //scanSummary.UpdateCorrectFile(0);
        //scanSummary.UpdateFailedFile(new ScanIssue("C:\\path\\to\\file", ScanIssueError.FileError, "Error message", "Technical error message"));
        //Assert.NotEmpty(scanSummary.Issues);
        scanSummary.UpdateFoundFiles();
        scanSummary.UpdateFoundFiles();
        scanSummary.UpdateFoundFiles();
        scanSummary.UpdateFoundFiles();
        scanSummary.UpdateFoundFiles();
        scanSummary.UpdateCorrectFile(0);
        scanSummary.UpdateFailedFile(new ScanIssue("C:\\path\\to\\file", null, ScanIssueError.FileError, "Error message", "Technical error message"));
        scanSummary.UpdateScanStatus(ScanStatus.Cancelled);
        Assert.True(scanSummary.EndTime > DateTime.MinValue);
    }
    [Fact]
    public void UpdateScanStatus_WhenNotStartedAndCancelled_ChangesStatusToCancelled()
    {
        // Arrange
        ScanSummary summary = new ScanSummary();

        // Act
        summary.UpdateScanStatus(ScanStatus.Cancelled);

        // Assert
        Assert.Equal(ScanStatus.Cancelled, summary.Status);
    }

    [Fact]
    public void UpdateScanStatus_WhenNotStartedAndCancelled_SetsEndTime()
    {
        // Arrange
        ScanSummary summary = new ScanSummary();

        // Act
        summary.UpdateScanStatus(ScanStatus.Cancelled);

        // Assert
        Assert.NotNull(summary.EndTime);
    }
    [Fact]
    public void UpdateScanStatus_WhenCompletedFromNotStarted_ThrowsInvalidOperationException()
    {
        // Arrange
        ScanSummary summary = new ScanSummary();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => summary.UpdateScanStatus(ScanStatus.Completed));
    }

    [Fact]
    public void UpdateScanStatus_WhenCancelledTwice_ThrowsInvalidOperationException()
    {
        // Arrange
        ScanSummary summary = new ScanSummary();
        summary.UpdateScanStatus(ScanStatus.Cancelled);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => summary.UpdateScanStatus(ScanStatus.Cancelled));
    }
}
