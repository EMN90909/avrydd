using Avryd.Core.Navigation;
using Avryd.Core.Plugins;
using Avryd.Core.Speech;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Avryd.App.Windows;

public partial class MainWindow : Window
{
    private bool _isPaused;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;

        App.Navigation.ModeChanged += (s, mode) =>
            Dispatcher.Invoke(() => ModeLabel.Text = mode.ToString() + " Mode");

        App.Session.SessionTick += (s, duration) =>
            Dispatcher.Invoke(UpdateStats);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateStats();
        LoadSettings();
        LoadVoices();
        LoadLanguages();
        LoadPlugins();

        var profile = App.Settings.Profile;
        var name = profile?.DisplayName ?? profile?.Email ?? "User";
        GreetingText.Text = $"Hello, {name}. Avryd is protecting your screen.";

        if (profile != null)
        {
            AccountEmail.Text = profile.Email;
            AccountStatus.Text = $"Activated · {profile.Provider} sign-in";
            AccountStats.Text = App.Session.GetUsageSummary();
        }

        ShowPanel("dashboard");
    }

    private void UpdateStats()
    {
        var total = TimeSpan.FromMinutes(App.Session.TotalUsageMinutes);
        StatSessions.Text = App.Session.TotalSessionCount.ToString();
        StatTime.Text = $"{(int)total.TotalHours}h {total.Minutes}m";
        StatPlugins.Text = App.Plugins.LoadedPlugins.Count.ToString();
    }

    private void LoadSettings()
    {
        var s = App.Settings.Settings;
        RateSlider.Value = s.Rate;
        VolumeSlider.Value = s.Volume;
        ChkStartup.IsChecked = s.LaunchAtStartup;
        ChkMinimize.IsChecked = s.MinimizeToTray;
        ChkNotifications.IsChecked = s.SpeakNotifications;
        ChkTooltips.IsChecked = s.SpeakTooltips;
        ChkOcr.IsChecked = s.OcrEnabled;
        ChkBraille.IsChecked = s.BrailleEnabled;
        ChkFocusHighlight.IsChecked = s.FocusHighlight;

        VoiceCommandStatus.Text = s.VoiceCommandsEnabled
            ? "Enabled — say a command to control Avryd"
            : "Disabled — enable in Settings";
        VoiceToggleBtn.Content = s.VoiceCommandsEnabled ? "Disable" : "Enable";
    }

    private void LoadVoices()
    {
        var voices = App.Speech.GetAvailableVoices();
        VoiceCombo.ItemsSource = voices;
        if (!string.IsNullOrEmpty(App.Settings.Settings.VoiceId))
            VoiceCombo.SelectedItem = App.Settings.Settings.VoiceId;
        else if (voices.Any())
            VoiceCombo.SelectedIndex = 0;

        VoicesList.ItemsSource = voices;
    }

    private void LoadLanguages()
    {
        VoicesList.ItemsSource = App.Speech.GetAvailableVoices();
    }

    private async void LoadPlugins()
    {
        InstalledPluginsList.ItemsSource = App.Plugins.LoadedPlugins;
        try
        {
            var available = await App.Plugins.FetchAvailablePluginsAsync();
            AvailablePluginsList.ItemsSource = available.Where(p => !p.IsInstalled).ToList();
        }
        catch { }
    }

    private void ShowPanel(string panel)
    {
        PanelDashboard.Visibility = panel == "dashboard" ? Visibility.Visible : Visibility.Collapsed;
        PanelSettings.Visibility = panel == "settings" ? Visibility.Visible : Visibility.Collapsed;
        PanelPlugins.Visibility = panel == "plugins" ? Visibility.Visible : Visibility.Collapsed;
        PanelLanguages.Visibility = panel == "languages" ? Visibility.Visible : Visibility.Collapsed;
        PanelShortcuts.Visibility = panel == "shortcuts" ? Visibility.Visible : Visibility.Collapsed;
        PanelProfile.Visibility = panel == "profile" ? Visibility.Visible : Visibility.Collapsed;
        PanelHelp.Visibility = panel == "help" ? Visibility.Visible : Visibility.Collapsed;

        App.Speech.Speak(panel + " panel", SpeechPriority.Normal);
    }

    // Nav clicks
    private void NavDashboard_Click(object s, RoutedEventArgs e) => ShowPanel("dashboard");
    private void NavSettings_Click(object s, RoutedEventArgs e) => ShowPanel("settings");
    private void NavPlugins_Click(object s, RoutedEventArgs e) { ShowPanel("plugins"); LoadPlugins(); }
    private void NavLanguages_Click(object s, RoutedEventArgs e) { ShowPanel("languages"); LoadLanguages(); }
    private void NavShortcuts_Click(object s, RoutedEventArgs e) => ShowPanel("shortcuts");
    private void NavProfile_Click(object s, RoutedEventArgs e) { ShowPanel("profile"); AccountStats.Text = App.Session.GetUsageSummary(); }
    private void NavHelp_Click(object s, RoutedEventArgs e) => ShowPanel("help");

    // Quick actions
    private void ReadCurrentItem_Click(object s, RoutedEventArgs e) => App.Focus.ReadCurrentElement();
    private void StopSpeaking_Click(object s, RoutedEventArgs e) => App.Speech.Stop();

    private void ToggleBtn_Click(object s, RoutedEventArgs e)
    {
        _isPaused = !_isPaused;
        if (_isPaused)
        {
            App.Speech.Pause();
            ToggleBtn.Content = "▶  Resume Avryd";
            StatusLabel.Text = "Paused";
            StatusDot.Fill = System.Windows.Media.Brushes.Orange;
        }
        else
        {
            App.Speech.Resume();
            ToggleBtn.Content = "⏸  Pause Avryd";
            StatusLabel.Text = "Active";
            StatusDot.Fill = (System.Windows.Media.Brush)FindResource("SuccessBrush");
        }
    }

    private void QuitBtn_Click(object s, RoutedEventArgs e)
    {
        if (MessageBox.Show("Quit Avryd? Screen reading will stop.", "Quit Avryd",
            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            App.Speech.SpeakImmediate("Goodbye");
            System.Threading.Thread.Sleep(1200);
            Application.Current.Shutdown();
        }
    }

    private void VoiceToggleBtn_Click(object s, RoutedEventArgs e)
    {
        var enabled = !App.Settings.Settings.VoiceCommandsEnabled;
        App.Settings.Settings.VoiceCommandsEnabled = enabled;
        App.Settings.SaveSettings();

        if (enabled) App.VoiceCommands.Enable();
        else App.VoiceCommands.Disable();

        VoiceCommandStatus.Text = enabled
            ? "Enabled — say a command to control Avryd"
            : "Disabled — enable in Settings";
        VoiceToggleBtn.Content = enabled ? "Disable" : "Enable";
    }

    // Settings
    private void VoiceCombo_SelectionChanged(object s, SelectionChangedEventArgs e)
    {
        if (VoiceCombo.SelectedItem is string voice)
        {
            App.Settings.Settings.VoiceId = voice;
            App.Settings.SaveSettings();
        }
    }

    private void RateSlider_ValueChanged(object s, RoutedPropertyChangedEventArgs<double> e)
    {
        var rate = (int)e.NewValue;
        App.Settings.Settings.Rate = rate;
        if (RateDisplay != null) RateDisplay.Text = $"{rate} wpm";
        App.Settings.SaveSettings();
    }

    private void VolumeSlider_ValueChanged(object s, RoutedPropertyChangedEventArgs<double> e)
    {
        App.Settings.Settings.Volume = e.NewValue;
        if (VolumeDisplay != null) VolumeDisplay.Text = $"{(int)(e.NewValue * 100)}%";
        App.Settings.SaveSettings();
    }

    private void Setting_Changed(object s, RoutedEventArgs e)
    {
        var cfg = App.Settings.Settings;
        cfg.LaunchAtStartup = ChkStartup.IsChecked == true;
        cfg.MinimizeToTray = ChkMinimize.IsChecked == true;
        cfg.SpeakNotifications = ChkNotifications.IsChecked == true;
        cfg.SpeakTooltips = ChkTooltips.IsChecked == true;
        cfg.OcrEnabled = ChkOcr.IsChecked == true;
        cfg.BrailleEnabled = ChkBraille.IsChecked == true;
        cfg.FocusHighlight = ChkFocusHighlight.IsChecked == true;
        App.Settings.SaveSettings();

        if (cfg.LaunchAtStartup) SetStartup(true);
        else SetStartup(false);
    }

    private void ProfileBtn_Click(object s, RoutedEventArgs e)
    {
        if (s is Button btn && btn.Tag is string profile)
        {
            App.Settings.ApplyProfile(profile);
            LoadSettings();
            App.Speech.Speak($"Profile set to {profile}");
        }
    }

    private void ExportSettings_Click(object s, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog { Filter = "JSON|*.json", FileName = "avryd_settings.json" };
        if (dlg.ShowDialog() == true)
        {
            File.WriteAllText(dlg.FileName, App.Settings.ExportSettings());
            App.Speech.Speak("Settings exported");
        }
    }

    private void ImportSettings_Click(object s, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "JSON|*.json" };
        if (dlg.ShowDialog() == true)
        {
            App.Settings.ImportSettings(File.ReadAllText(dlg.FileName));
            LoadSettings();
            App.Speech.Speak("Settings imported");
        }
    }

    // Plugins
    private async void RefreshPlugins_Click(object s, RoutedEventArgs e) => await LoadPluginsAsync();

    private async Task LoadPluginsAsync()
    {
        InstalledPluginsList.ItemsSource = App.Plugins.LoadedPlugins;
        var available = await App.Plugins.FetchAvailablePluginsAsync();
        AvailablePluginsList.ItemsSource = available.Where(p => !p.IsInstalled).ToList();
    }

    private async void InstallPlugin_Click(object s, RoutedEventArgs e)
    {
        if (s is Button btn && btn.Tag is PluginInfo info)
        {
            App.Speech.Speak($"Installing {info.Name}...");
            btn.IsEnabled = false;
            var ok = await App.Plugins.InstallPluginAsync(info);
            btn.IsEnabled = true;
            App.Speech.Speak(ok ? $"{info.Name} installed" : $"Failed to install {info.Name}");
            await LoadPluginsAsync();
        }
    }

    private void RemovePlugin_Click(object s, RoutedEventArgs e)
    {
        if (s is Button btn && btn.Tag is string id)
        {
            App.Plugins.RemovePlugin(id);
            LoadPlugins();
            App.Speech.Speak("Plugin removed");
        }
    }

    // Languages
    private void UseVoice_Click(object s, RoutedEventArgs e)
    {
        if (s is Button btn && btn.Tag is string voice)
        {
            App.Settings.Settings.VoiceId = voice;
            App.Settings.SaveSettings();
            App.Speech.Speak($"Voice set to {voice}");
        }
    }

    // Account
    private void SignOut_Click(object s, RoutedEventArgs e)
    {
        if (MessageBox.Show("Sign out of Avryd?", "Sign Out",
            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            App.Auth.Logout();
            App.Speech.SpeakImmediate("Signed out");
            Application.Current.Shutdown();
        }
    }

    private static void SetStartup(bool enable)
    {
        var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
        if (key == null) return;
        if (enable)
            key.SetValue("Avryd", $"\"{System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName}\"");
        else
            key.DeleteValue("Avryd", false);
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (App.Settings.Settings.MinimizeToTray)
        {
            e.Cancel = true;
            Hide();
        }
        else
        {
            base.OnClosing(e);
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (App.Settings.Settings.MinimizeToTray) { e.Cancel = true; Hide(); }
    }
}
