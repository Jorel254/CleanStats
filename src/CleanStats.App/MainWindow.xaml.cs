using CleanStats.Core.Contracts;
using CleanStats.Core.Model;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Win32;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
namespace CleanStats.App;

public partial class MainWindow : System.Windows.Window
{
    private readonly IScanCoordinator _scanCoordinator;
    public ObservableCollection<FileScanRow> rows { get; set; }

    public ChartsGraph chartsGraph { get; } = new();
    public ChartsGraph rowGraph { get; } = new();
    public ChartsGraph storageGraph { get; } = new();
    private long _lastProgressUiUpdate;

    public MainWindow(IScanCoordinator scanCoordinator)
    {
        InitializeComponent();
        DataContext = this;
        if (scanCoordinator == null)
        {
            throw new ArgumentNullException(nameof(scanCoordinator));
        }
        _scanCoordinator = scanCoordinator;
        _scanCoordinator.ProgressChanged += EventProgress;
    }
    private bool IncludeHiddenFiles => ChkOcultos.IsChecked == true;

    private void BtnCambiarRuta_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Seleccionar carpeta para analizar",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            TxtRuta.Text = dialog.FolderName;
        }
    }

    private async void BtnIniciar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtRuta.Text) || !Directory.Exists(TxtRuta.Text))
        {
            MessageBox.Show("Por favor, seleccione una ruta válida para iniciar el escaneo.", "Ruta inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var options = new ScanOptions(TxtRuta.Text, [], IncludeHiddenFiles);
        rows = new ObservableCollection<FileScanRow>();
        BtnIniciar.IsEnabled = false;
        BtnCancelar.IsEnabled = true;
        try
        {
            var summary = await _scanCoordinator.StartScanAsync(options, CancellationToken.None);
            TxtEncontrados.Text = summary.FilesFound.ToString();
            TxtProcesados.Text = summary.FilesProcessed.ToString();
            TxtErrores.Text = summary.FilesFailed.ToString();
            var result = _scanCoordinator.CurrentResult;
            if (result != null)
            {
                var dictionaryClassificationResults =
                    result.Classifications.ToDictionary(cr => cr.FileEntryId);

                foreach (var fileEntry in result.FileEntries)
                {
                    if (!dictionaryClassificationResults.TryGetValue(
                            fileEntry.Id, out var classificationResult))
                    {
                        continue;
                    }

                    var fileScanRow = ConvertToFileScanRow(
                        fileEntry, classificationResult);

                    rows.Add(fileScanRow);
                }
            }
            ListaArchivos.ItemsSource = rows;
            TxtEstado.Text = summary.Status.ToString();
            LoadSummary(summary);
            LoadFileRows(rows);
            LoadStorageByCategory(rows);
        }
        finally
        {
            BtnIniciar.IsEnabled = true;
            BtnCancelar.IsEnabled = false;
        }
    }


    private void EventProgress(object sender, NotificationProgress e)
    {
        long currentTime = Environment.TickCount64;

        if (currentTime - _lastProgressUiUpdate < 200)
            return;

        _lastProgressUiUpdate = currentTime;

        Dispatcher.BeginInvoke(() =>
        {
            TxtEncontrados.Text = e.Summary.FilesFound.ToString();
            TxtProcesados.Text = e.Summary.FilesProcessed.ToString();
            TxtErrores.Text = e.Summary.FilesFailed.ToString();
        });
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _scanCoordinator.CancelScan();
            TxtEstado.Text = "Cancelando...";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cancelar el escaneo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private FileScanRow ConvertToFileScanRow(FileEntry fileEntry, ClassificationResult classificationResult)
    {
        if (fileEntry == null)
        {
            throw new ArgumentNullException(nameof(fileEntry));
        }
        if (classificationResult == null)
        {
            throw new ArgumentNullException(nameof(classificationResult));
        }
        return new FileScanRow(
            fileEntry.FileName,
            fileEntry.FilePath,
            classificationResult.Category,
            classificationResult.Status,
            fileEntry.Size,
            ConvertSizeToReadableFormat(fileEntry.Size),
            ConvertStatusToReadableFormat(classificationResult.Status),
            ConvertCategoryToReadableFormat(classificationResult.Category)
        );
    }
    private string ConvertSizeToReadableFormat(long sizeInBytes)
    {
        if (sizeInBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeInBytes), "El tamaño del archivo no puede ser negativo.");
        }
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = sizeInBytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##}{sizes[order]}";
    }
    private string ConvertStatusToReadableFormat(ClassificationStatus status)
    {
        return status switch
        {
            ClassificationStatus.Classified => "Clasificado",
            ClassificationStatus.Unclassified => "Sin clasificar",
            ClassificationStatus.PendingReview => "Pendiente de revisión",
            _ => "Desconocido",
        };
    }
    private string ConvertCategoryToReadableFormat(ClassificationCategory category)
    {
        return category switch
        {
            ClassificationCategory.Temporary => "Temporal",
            ClassificationCategory.Dump => "Dump",
            ClassificationCategory.Cache => "Cache",
            ClassificationCategory.Log => "Log",
            ClassificationCategory.Other => "Otro",
            _ => "Desconocido",
        };
    }
    private static SolidColorPaint GetCategoryColor(string category)
    {
        return category switch
        {
            "Temporary" => new(SKColors.DeepSkyBlue),
            "Cache" => new(SKColors.YellowGreen),
            "Log" => new(SKColors.Orange),
            "Dump" => new(SKColors.MediumPurple),
            "Other" => new(SKColors.SteelBlue),
            _ => new(SKColors.Gray)
        };
    }
    private static SolidColorPaint GetFileColor(string category)
    {
        return category switch
        {
            "Encontrados" => new(SKColors.DeepSkyBlue),
            "Procesados" => new(SKColors.YellowGreen),
            "Errores" => new(SKColors.Orange),
            _ => new(SKColors.Gray)
        };
    }
    public void LoadSummary(ScanSummary summary)
    {
        var valores = new List<ItemData>
    {
        new() { nombre = "Encontrados", valor = summary.FilesFound, color = GetFileColor("Encontrados") },
        new() { nombre = "Procesados", valor = summary.FilesProcessed, color = GetFileColor("Procesados") },
        new() { nombre = "Errores", valor = summary.FilesFailed, color = GetFileColor("Errores") },
    };

        chartsGraph.ColumnChart(valores);
    }
    public void LoadFileRows(ObservableCollection<FileScanRow> fileScanRows)
    {

        var filesByCategory = fileScanRows
             .GroupBy(row => row.Category)
             .Select(gp => new ItemData
             {
                 nombre = gp.Key.ToString(),
                 valor = gp.Count(),
                 color = GetCategoryColor(gp.Key.ToString())
             })
             .ToList();

        rowGraph.PieChart(filesByCategory, false);
    }
    public void LoadStorageByCategory(
    ObservableCollection<FileScanRow> fileScanRows)
    {
        var storageByCategory = fileScanRows
            .GroupBy(row => row.Category)
            .Select(gp => new ItemData
            {
                nombre = gp.Key.ToString(),
                valor = gp.Sum(row => row.FileSize),
                color = GetCategoryColor(gp.Key.ToString())
            })
            .ToList();

        storageGraph.PieChart(storageByCategory, true);
    }
}
