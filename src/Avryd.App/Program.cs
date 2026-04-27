using System.Threading;
using System.Windows;

namespace Avryd.App;

internal static class Program
{
    private static Mutex? _mutex;

    [STAThread]
    public static void Main(string[] args)
    {
        _mutex = new Mutex(true, "Avryd_SingleInstance", out var isNew);
        if (!isNew)
        {
            MessageBox.Show("Avryd is already running.", "Avryd", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
        finally
        {
            _mutex.ReleaseMutex();
        }
    }
}
