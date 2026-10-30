using CleanStats.Core;
using CleanStats.Core.Contracts;
using CleanStats.Infrastructure;

namespace CleanStats.App;

public partial class App : System.Windows.Application
{
    private IScanCoordinator? _scanCoordinator;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        IFileClassifier classifier = new FileClassifier();
        IFileScanner scanner = new FileScanner(classifier);

        _scanCoordinator = new ScanCoordinator(scanner);

        MainWindow mainWindow = new MainWindow(_scanCoordinator);
        mainWindow.Show();
    }
}
