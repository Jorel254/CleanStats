using CleanStats.Core.Contracts;
using CleanStats.Core.Model;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
namespace CleanStats.App;

public partial class MainWindow : System.Windows.Window
{
    private readonly IScanCoordinator _scanCoordinator;
    public ObservableCollection<FileScanRow> rows { get; set; }

    public MainWindow(IScanCoordinator scanCoordinator)
    {
        InitializeComponent();
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
        }
        finally
        {
            BtnIniciar.IsEnabled = true;
            BtnCancelar.IsEnabled = false;
        }
    }


    private void EventProgress(object sender, NotificationProgress e)
    {
        Dispatcher.Invoke(() =>
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
            fileEntry.Size
        );
    }
}
