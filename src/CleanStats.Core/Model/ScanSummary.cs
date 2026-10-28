using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Text;

namespace CleanStats.Core.Model;

public enum ScanStatus
{
    NotStarted,
    Cancelled,
    InProgress,
    Completed,
    Failed,
}
public class ScanSummary
{
    public Guid Id { get; }
    public ScanStatus Status { get { return _Status; } protected set { _Status = value; } }
    private ScanStatus _Status;
    public DateTime StartTime { get { return _StartTime; } protected set { _StartTime = value; } }
    private DateTime _StartTime;
    public DateTime? EndTime { get { return _EndTime; } protected set { _EndTime = value; } }
    private DateTime? _EndTime;
    public long FilesFound { get { return _FilesFound; } protected set { _FilesFound = value; } }
    private long _FilesFound;
    public long FilesProcessed { get { return _FilesProcessed; } protected set { _FilesProcessed = value; } }
    private long _FilesProcessed;
    public long FilesFailed { get { return _FilesFailed; } protected set { _FilesFailed = value; } }
    private long _FilesFailed;
    public long TotalBytesScanned { get { return _TotalBytesScanned; } protected set { _TotalBytesScanned = value; } }
    private long _TotalBytesScanned;
    private readonly ObservableCollection<ScanIssue> _issues;

    public ReadOnlyObservableCollection<ScanIssue> Issues { get; }


    public ScanSummary()
    {
        Id = Guid.NewGuid();
        _Status = ScanStatus.NotStarted;
        _issues = new ObservableCollection<ScanIssue>();
        Issues = new ReadOnlyObservableCollection<ScanIssue>(_issues);
    }
    public void StartScan()
    {
        if (Status != ScanStatus.NotStarted)
        {
            throw new InvalidOperationException("No se puede iniciar un escaneo que ya está en curso o ha finalizado.");
        }
        _Status = ScanStatus.InProgress;
        StartTime = DateTime.UtcNow;
    }
    public void UpdateFoundFiles()
    {
        if (Status != ScanStatus.InProgress)
        {
            throw new InvalidOperationException("No se puede actualizar un escaneo que no está en curso.");
        }

        _FilesFound += 1;

    }

    public void UpdateCorrectFile(long totalBytesScanned)
    {
        if (Status != ScanStatus.InProgress)
        {
            throw new InvalidOperationException("No se puede actualizar un escaneo que no está en curso.");
        }
        if (totalBytesScanned >= 0)
        {
            _TotalBytesScanned += totalBytesScanned;
            _FilesProcessed += 1;
        }
        else
        {
            throw new ArgumentException("El número de bytes o el número de archivos contiene un valor no válido.");
        }
    }
    public void UpdateFailedFile(ScanIssue issue)
    {
        if (Status != ScanStatus.InProgress)
        {
            throw new InvalidOperationException("No se puede actualizar un escaneo que no está en curso.");
        }
        if (issue != null)
        {
            _FilesFailed += 1;
            _issues.Add(issue);
        }
        else
        {
            throw new ArgumentException("El número de archivos o el problema de escaneo contiene un valor no válido.");
        }
    }
    public void AddDirectoryIssue(ScanIssue issue)
    {
        if (Status != ScanStatus.InProgress)
        {
            throw new InvalidOperationException("No se puede actualizar un escaneo que no está en curso.");
        }
        if (issue == null)
        {
            throw new ArgumentException("El problema de escaneo es nulo.");
           
        }
        if (issue.ErrorType != ScanIssueError.DirectoryError)
        {
           throw new ArgumentException("El problema de escaneo no es un error de directorio.");
        }
        _issues.Add(issue);
    }
    public void UpdateScanStatus(ScanStatus status)
    {
        if (status == ScanStatus.Completed && Status == ScanStatus.InProgress)
        {
            if (FilesFound != FilesProcessed + FilesFailed)
            {
                throw new InvalidOperationException("No se puede completar el escaneo, ya que el número de archivos encontrados no coincide con la suma de los archivos procesados y fallidos.");
            }
            else
            {
                _Status = ScanStatus.Completed;
            }
        }
        else if (status == ScanStatus.Cancelled && Status == ScanStatus.InProgress)
        {
            _Status = ScanStatus.Cancelled;
        }
        else if (status == ScanStatus.Failed && Status == ScanStatus.InProgress)
        {
            _Status = ScanStatus.Failed;
        }
        else
        {
            throw new InvalidOperationException("No se puede actualizar el estado de un escaneo que no está en curso.");
        }
        if (status == ScanStatus.Completed || status == ScanStatus.Cancelled || status == ScanStatus.Failed)
            _EndTime = DateTime.UtcNow;
    }

}
