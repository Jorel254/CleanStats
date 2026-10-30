using CleanStats.Core.Model;

namespace CleanStats.Core.Contracts;

public interface IFileClassifier
{
    ClassificationResult ClassifyFile(FileEntry fileEntry);
}
