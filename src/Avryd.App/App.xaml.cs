using Avryd.Core.Auth;
using Avryd.Core.Commands;
using Avryd.Core.Fallback;
using Avryd.Core.Focus;
using Avryd.Core.Input;
using Avryd.Core.Navigation;
using Avryd.Core.Notifications;
using Avryd.Core.OCR;
using Avryd.Core.Plugins;
using Avryd.Core.Session;
using Avryd.Core.Settings;
using Avryd.Core.Speech;
using Avryd.Core.UIA;
using Avryd.App.Windows;
using System.IO;
using System.Windows;

namespace Avryd.App;

public partial class App : Application
{
    public static SettingsManager Settings { get; private set; } = null!;
    public static SpeechManager Speech { get; private set; } = null!;
    public static UIAManager UIA { get; private set; } = null!;
    public static FocusTracker Focus { get; private set; } = null!;
    public static NavigationManager Navigation { get; private set; } = null!;
    public static KeyboardManager Keyboard { get; private set; } = null!;
    public static GestureManager Gesture { get; private set; } = null!;
    public static BrailleManager Braille { get; private set; } = null!;
    public static PluginManager Plugins { get; private set; } = null!;
    public static OCRManager OCR { get; private set; } = null!;
    public static FallbackReader Fallback { get; private set; } = null!;
    public static SessionManager Session { get; private set; } = null!;
    public static NotificationManager Notifications { get; private set; } = null!;
    public static VoiceCommandManager VoiceCommands { get; private set; } = null!;
    public static AuthManager Auth { get; private set; } = null!;

    private Controls.TrayIcon? _trayIcon;
    private bool _coreStarted;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (s, ex) =>
        {
            MessageBox.Show($"Unexpected error: {ex.Exception.Message}", "Avryd Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            ex.Handled = true;
        };

        Settings = new SettingsManager();
        Settings.Load();

        Auth = new AuthManager(Settings);

        if (Settings.IsFirstRun || !Settings.IsActivated)
        {
            ShowLoginFlow();
        }
        else
        {
            StartCore();
            ShowMainWindow();
        }
    }

    public void ShowLoginFlow()
    {
        var login = new LoginWindow();
        login.LoginSucceeded += (s, profile) =>
        {
            login.Close();
            if (Settings.IsFirstRun)
            {
                var onboarding = new OnboardingWindow();
                onboarding.Completed += (_, __) =>
                {
                    onboarding.Close();
                    StartCore();
                    ShowMainWindow();
                };
                onboarding.Show();
            }
            else
            {
                StartCore();
                ShowMainWindow();
            }
        };
        login.Show();
    }

    public void StartCore()
    {
        if (_coreStarted) return;
        _coreStarted = true;

        var s = Settings.Settings;
        var piperExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "piper", "piper.exe");
        var piperModels = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "piper", "voices");
        var tessData = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "tessdata");
        var pluginsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");

        Speech = new SpeechManager(Settings, piperExe, piperModels);
        Speech.Start();

        UIA = new UIAManager();
        UIA.Start();

        Focus = new FocusTracker(UIA, Speech);
        Navigation = new NavigationManager(UIA, Speech, Focus);
        Keyboard = new KeyboardManager(Speech, Navigation);

        if (s.HotkeysEnabled) Keyboard.Install();

        Gesture = new GestureManager(Speech, Navigation, UIA);
        Braille = new BrailleManager(Speech);

        if (s.BrailleEnabled) Braille.Enable();

        OCR = new OCRManager(tessData);
        Fallback = new FallbackReader(OCR, Speech, UIA);
        Fallback.Start();

        Plugins = new PluginManager(Speech, pluginsDir);
        Plugins.LoadInstalled();

        Session = new SessionManager(Settings);
        Session.StartSession();

        Notifications = new NotificationManager(Speech);

        VoiceCommands = new VoiceCommandManager(Speech, Navigation);
        if (s.VoiceCommandsEnabled) VoiceCommands.Enable();

        _trayIcon = new Controls.TrayIcon();
        _trayIcon.Initialize();

        Speech.Speak("Avryd is ready", SpeechPriority.High);
    }

    private void ShowMainWindow()
    {
        var main = new MainWindow();
        main.Show();
        MainWindow = main;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Session?.Dispose();
        Keyboard?.Dispose();
        Navigation?.Dispose();
        Focus?.Dispose();
        Fallback?.Dispose();
        Plugins?.Dispose();
        Notifications?.Dispose();
        VoiceCommands?.Dispose();
        Braille?.Dispose();
        Gesture?.Dispose();
        UIA?.Dispose();
        OCR?.Dispose();
        Speech?.Dispose();
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
