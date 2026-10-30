
namespace CleanStats.Core.Model;

public class FileScanRow
{
    public string FileName { get; }
    public string FilePath { get; }
    public ClassificationCategory Category { get; }
    public ClassificationStatus Status { get; }
    public long FileSize { get; }

    public FileScanRow(
        string fileName,
        string filePath,
        ClassificationCategory category,
        ClassificationStatus status,
        long fileSize)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("El nombre del archivo es obligatorio.", nameof(fileName));

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("La ruta del archivo es obligatoria.", nameof(filePath));

        if (fileSize < 0)
            throw new ArgumentOutOfRangeException(nameof(fileSize));

        FileName = fileName;
        FilePath = filePath;
        Category = category;
        Status = status;
        FileSize = fileSize;
    }
}
