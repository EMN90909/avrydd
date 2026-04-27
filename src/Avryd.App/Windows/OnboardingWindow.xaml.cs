using System.Windows;
using System.Windows.Controls;

namespace Avryd.App.Windows;

public partial class OnboardingWindow : Window
{
    private int _currentStep = 0;
    private readonly string[] _steps = { "voice", "shortcuts", "profile", "done" };

    public event EventHandler? Completed;

    public OnboardingWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var voices = App.Speech.GetAvailableVoices();
        VoiceCombo.ItemsSource = voices;
        if (voices.Any())
            VoiceCombo.SelectedIndex = 0;

        UpdateStep();
        App.Speech.Speak("Welcome to Avryd setup. Let's get started.", Avryd.Core.Speech.SpeechPriority.High);
    }

    private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void UpdateStep()
    {
        StepVoice.Visibility = _currentStep == 0 ? Visibility.Visible : Visibility.Collapsed;
        StepShortcuts.Visibility = _currentStep == 1 ? Visibility.Visible : Visibility.Collapsed;
        StepProfile.Visibility = _currentStep == 2 ? Visibility.Visible : Visibility.Collapsed;
        StepDone.Visibility = _currentStep == 3 ? Visibility.Visible : Visibility.Collapsed;

        StepIndicator.Text = $"Step {_currentStep + 1} of {_steps.Length}";
        BackBtn.IsEnabled = _currentStep > 0;
        NextBtn.Content = _currentStep == _steps.Length - 1 ? "Finish ✓" : "Next →";

        var stepNames = new[] { "Voice setup", "Shortcuts", "Profile selection", "Setup complete" };
        App.Speech.Speak(stepNames[_currentStep], Avryd.Core.Speech.SpeechPriority.Normal);
    }

    private void NextBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_currentStep == _steps.Length - 1)
        {
            ApplySettings();
            Completed?.Invoke(this, EventArgs.Empty);
            return;
        }
        _currentStep++;
        UpdateStep();
    }

    private void BackBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_currentStep > 0) { _currentStep--; UpdateStep(); }
    }

    private void SkipBtn_Click(object sender, RoutedEventArgs e)
    {
        App.Speech.Speak("Setup skipped. Avryd is ready.");
        Completed?.Invoke(this, EventArgs.Empty);
    }

    private void VoiceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (VoiceCombo.SelectedItem is string voice)
            App.Settings.Settings.VoiceId = voice;
    }

    private void RateSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        var rate = (int)e.NewValue;
        App.Settings.Settings.Rate = rate;
        if (RateLabel != null) RateLabel.Text = $"{rate} words per minute";
    }

    private void TestVoice_Click(object sender, RoutedEventArgs e)
    {
        App.Speech.SpeakImmediate("Hello, I am Avryd, your screen reader. I will read your screen and help you navigate.");
    }

    private void ApplySettings()
    {
        // Apply selected profile
        string profile = "standard";
        if (ProfileFast.IsChecked == true) profile = "fast";
        else if (ProfileNewUser.IsChecked == true) profile = "new_user";
        else if (ProfileQuiet.IsChecked == true) profile = "quiet";

        App.Settings.ApplyProfile(profile);
        App.Settings.SaveSettings();
        App.Speech.Speak("Avryd is now set up and ready.");
    }
}
