using System;
using System.Collections.Generic;
using System.Text;
using CleanStats.Core.Model;
namespace CleanStats.Core.Tests;

public class ScanIssueTests
{
    [Fact]
    public void ValidateClass()
    {
        var scanIssue = new ScanIssue(null, null, ScanIssueError.GeneralError, "Error message", "Technical error message");
        var scanIssue2 = new ScanIssue("C:\\path\\to\\file", null, ScanIssueError.FileError, "Error message", "Technical error message");
        var scanIssue3 = new ScanIssue(null, "C:\\path\\to\\directory", ScanIssueError.DirectoryError, "Error message", "Technical error message");
        var scanIssue4 = new ScanIssue("C:\\path\\to\\file", null, ScanIssueError.FileError, null, null);
        Assert.NotEqual(scanIssue.Id, scanIssue2.Id);
        Assert.NotNull(scanIssue);
        Assert.NotNull(scanIssue2);
        Assert.NotNull(scanIssue3);
        Assert.NotNull(scanIssue4);
    }
}
