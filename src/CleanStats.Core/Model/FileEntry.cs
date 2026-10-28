using System;
using System.Collections.Generic;
using System.Text;

namespace CleanStats.Core.Model
{
    public class FileEntry
    {

        public Guid Id { get;  }
        public string FilePath { get; }
        public long Size { get; }
        public string? FileName { get; }
        public string? FileExtension { get; }
        public DateTime? CreateDate { get; }
        public DateTime? LastModified { get; }

        public FileEntry(string filePath, long size, DateTime? lastModified, DateTime? createDate)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("La ruta del archivo no puede ser nula o vacía.", nameof(filePath));
            }
            if (size < 0)
            {
                throw new ArgumentException("El tamaño del archivo no puede ser negativo.", nameof(size));
            }
            Id = Guid.NewGuid();
            FilePath = filePath;
            FileName = System.IO.Path.GetFileName(filePath);
            FileExtension = System.IO.Path.GetExtension(filePath);
            Size = size;
            LastModified = lastModified;
            CreateDate = createDate;
        }
    }
}
