using Avryd.App.Windows;
using Avryd.Core.Speech;
using Hardcodet.Wpf.TaskbarNotification;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Avryd.App.Controls;

public class TrayIcon : IDisposable
{
    private TaskbarIcon? _icon;
    private bool _disposed;

    public void Initialize()
    {
        _icon = new TaskbarIcon
        {
            ToolTipText = "Avryd Screen Reader",
            ContextMenu = BuildMenu()
        };

        // Try to load icon
        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "icon_64.ico");
        if (File.Exists(iconPath))
            _icon.Icon = new System.Drawing.Icon(iconPath);

        _icon.TrayMouseDoubleClick += (s, e) => OpenLauncher();
    }

    private ContextMenu BuildMenu()
    {
        var menu = new ContextMenu();

        var header = new MenuItem { Header = "Avryd Screen Reader", IsEnabled = false };
        header.FontWeight = System.Windows.FontWeights.Bold;

        var openItem = new MenuItem { Header = "Open Launcher (Ctrl+Alt+G)" };
        openItem.Click += (s, e) => OpenLauncher();

        var stopItem = new MenuItem { Header = "Stop Speaking" };
        stopItem.Click += (s, e) => App.Speech.Stop();

        var pauseItem = new MenuItem { Header = "Pause / Resume" };
        pauseItem.Click += (s, e) =>
        {
            if (App.Speech.IsPaused) App.Speech.Resume();
            else App.Speech.Pause();
        };

        var readItem = new MenuItem { Header = "Read Current Item" };
        readItem.Click += (s, e) => App.Focus.ReadCurrentElement();

        var settingsItem = new MenuItem { Header = "Settings" };
        settingsItem.Click += (s, e) => OpenLauncher();

        var sep = new Separator();

        var quitItem = new MenuItem { Header = "Quit Avryd" };
        quitItem.Click += (s, e) =>
        {
            App.Speech.SpeakImmediate("Goodbye");
            System.Threading.Thread.Sleep(1200);
            Application.Current.Shutdown();
        };

        menu.Items.Add(header);
        menu.Items.Add(new Separator());
        menu.Items.Add(openItem);
        menu.Items.Add(stopItem);
        menu.Items.Add(pauseItem);
        menu.Items.Add(readItem);
        menu.Items.Add(settingsItem);
        menu.Items.Add(sep);
        menu.Items.Add(quitItem);

        return menu;
    }

    private static void OpenLauncher()
    {
        if (Application.Current.MainWindow is MainWindow main)
        {
            main.Show();
            main.Activate();
        }
        else
        {
            var win = new MainWindow();
            win.Show();
            Application.Current.MainWindow = win;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _icon?.Dispose();
    }
}
