
namespace CleanStats.Core.Model;

public class FileScanRow
{
    public string FileName { get; }
    public string FilePath { get; }
    public ClassificationCategory Category { get; }
    public ClassificationStatus Status { get; }
    public long FileSize { get; }
    public string ReadableFileSize { get; }
    public string ReadableStatus { get; }
    public string ReadableCategory { get; }

    public FileScanRow(
        string fileName,
        string filePath,
        ClassificationCategory category,
        ClassificationStatus status,
        long fileSize,
        string readableFileSize,
        string readableStatus,
        string readableCategory)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("El nombre del archivo es obligatorio.", nameof(fileName));

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("La ruta del archivo es obligatoria.", nameof(filePath));

        if (fileSize < 0)
            throw new ArgumentOutOfRangeException(nameof(fileSize));

        if (string.IsNullOrWhiteSpace(readableFileSize))
            throw new ArgumentException("La ruta del archivo es obligatoria.", nameof(readableFileSize));

        if (string.IsNullOrWhiteSpace(readableStatus))
            throw new ArgumentException("La ruta del archivo es obligatoria.", nameof(readableStatus));

        if (string.IsNullOrWhiteSpace(readableCategory))
            throw new ArgumentException("La ruta del archivo es obligatoria.", nameof(readableCategory));

        FileName = fileName;
        FilePath = filePath;
        Category = category;
        Status = status;
        FileSize = fileSize;
        ReadableFileSize = readableFileSize;
        ReadableStatus = readableStatus;
        ReadableCategory = readableCategory;
    }
}
