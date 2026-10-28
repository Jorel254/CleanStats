using CleanStats.Core.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace CleanStats.Core.Progress;


public class ScanResult
{
    private readonly ObservableCollection<FileEntry> _fileEntries;

    public ReadOnlyObservableCollection<FileEntry> FileEntries { get; }
    private readonly ObservableCollection<ScanIssue> _issues;

    public ReadOnlyObservableCollection<ScanIssue> Issues { get; }

    public ScanStatus Status { get; }

    public ScanResult(FileEntry[] fileEntries, ScanIssue[] issues, ScanStatus status)
    {
        if (fileEntries == null)
        {
            throw new ArgumentNullException(nameof(fileEntries));
        }

        if (fileEntries.Any(fileEntry => fileEntry == null))
        {
            throw new ArgumentException("La colección de entradas de archivos no puede contener elementos nulos.", nameof(fileEntries));
        }

        if (issues == null)
        {
            throw new ArgumentNullException(nameof(issues));
        }

        if (issues.Any(issue => issue == null))
        {
            throw new ArgumentException("La colección de problemas no puede contener elementos nulos.", nameof(issues));
        }

        if (status == ScanStatus.NotStarted || status == ScanStatus.InProgress)
        {
            throw new ArgumentException("El estado del escaneo no es válido.", nameof(status));
        }

        if (!Enum.IsDefined(typeof(ScanStatus), status))
        {
            throw new ArgumentException("El estado del escaneo proporcionado no es válido.", nameof(status));
        }
        _fileEntries = new ObservableCollection<FileEntry>(fileEntries);
        FileEntries = new ReadOnlyObservableCollection<FileEntry>(_fileEntries);
        _issues = new ObservableCollection<ScanIssue>(issues);
        Issues = new ReadOnlyObservableCollection<ScanIssue>(_issues);
        Status = status;
    }
}
