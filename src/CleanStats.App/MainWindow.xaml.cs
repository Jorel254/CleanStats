using System.Windows;
using System.Windows.Media;
using CleanStats.Core.Contracts;
using Microsoft.Win32;
using CleanStats.Core.Model;
using System.Windows.Threading;
using System.IO;
namespace CleanStats.App;

public partial class MainWindow : System.Windows.Window
{
    private readonly IScanCoordinator _scanCoordinator;

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

        BtnIniciar.IsEnabled = false;
        BtnCancelar.IsEnabled = true;
        try
        {
            var result = await _scanCoordinator.StartScanAsync(options, CancellationToken.None);

            TxtEstado.Text = result.Status.ToString();
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
}
