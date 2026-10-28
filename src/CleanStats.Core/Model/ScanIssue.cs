using System;
using System.Collections.Generic;
using System.Text;

namespace CleanStats.Core.Model;

public enum ScanIssueError
{
    GeneralError,
    FileError,
    DirectoryError
}
public class ScanIssue
{
    public Guid Id { get; }
    public string? FilePath { get; }
    public string? DirectoryPath { get; }
    public ScanIssueError ErrorType { get; }
    public string? ErrorMessage { get; }
    public string? ErrorMessageTechnical { get; }
    public DateTime ScanAt { get; }

    public ScanIssue(string? filePath, string? directoryPath, ScanIssueError errorType, string? errorMessage, string? errorMessageTechnical)
    {
        if (!Enum.IsDefined(typeof(ScanIssueError), errorType))
        {
            throw new ArgumentException("El tipo de error proporcionado no es válido.", nameof(errorType));
        }
        ErrorType = errorType;
        switch(errorType)
        {
            case ScanIssueError.FileError:
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentException("La ruta del archivo no puede ser nula o vacía para un error de archivo.", nameof(filePath));
                }
                if(!string.IsNullOrWhiteSpace(directoryPath))
                {
                    throw new ArgumentException("La ruta del directorio debe ser nula o vacía para un error de archivo.", nameof(directoryPath));
                }
                FilePath = filePath;
                DirectoryPath = null; // No se requiere ruta de directorio para FileError
                break;
            case ScanIssueError.DirectoryError:
                if (string.IsNullOrWhiteSpace(directoryPath))
                {
                    throw new ArgumentException("La ruta del directorio no puede ser nula o vacía para un error de directorio.", nameof(directoryPath));
                }
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentException("La ruta del archivo debe ser nula o vacía para un error de directorio.", nameof(filePath));
                }
                DirectoryPath = directoryPath;
                FilePath = null; // No se requiere ruta de archivo para DirectoryError
                break;
            case ScanIssueError.GeneralError:
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentException("La ruta del archivo debe ser nula o vacía para un error de directorio.", nameof(filePath));
                }
                if (!string.IsNullOrWhiteSpace(directoryPath))
                {
                    throw new ArgumentException("La ruta del directorio debe ser nula o vacía para un error de archivo.", nameof(directoryPath));
                }
                DirectoryPath = null;
                FilePath = null;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(errorType), "Tipo de error desconocido.");
        }
        Id = Guid.NewGuid();
        ErrorMessage = errorMessage;
        ErrorMessageTechnical = errorMessageTechnical;
        ScanAt = DateTime.UtcNow;
    }
}
