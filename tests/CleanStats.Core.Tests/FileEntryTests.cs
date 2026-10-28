using CleanStats.Core.Model;
using Xunit;

namespace CleanStats.Core.Tests;

public class FileEntryTests
{
    [Fact]
    public void ValidateClass()
    {
        var fileEntry = new FileEntry("C:\\path\\to\\file", 1024, null, null);
        var fileEntry2 = new FileEntry("C:\\path\\to\\file", 1024, null, null);
        //Assert.Equal("C:\\path\\to\\file.txt", fileEntry.FilePath);
        //Assert.Equal(1024, fileEntry.Size);
        //Assert.Equal("file.txt", fileEntry.FileName);
        //Assert.Equal(".txt", fileEntry.FileExtension);
        Assert.NotEqual(fileEntry.Id, fileEntry2.Id);
        Assert.NotNull(fileEntry);
    }
}
