using Avryd.Core.Models;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace Avryd.App.Windows;

public partial class LoginWindow : Window
{
    public event EventHandler<UserProfile>? LoginSucceeded;

    public LoginWindow()
    {
        InitializeComponent();
        Loaded += (s, e) => EmailBox.Focus();

        App.Auth.LoginSuccess += OnLoginSuccess;
        App.Auth.LoginFailed += OnLoginFailed;
    }

    private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private async void GoogleBtn_Click(object sender, RoutedEventArgs e)
    {
        SetLoading(true, "Opening browser for Google sign-in...");
        await App.Auth.BeginOAuthAsync("google");
    }

    private async void MicrosoftBtn_Click(object sender, RoutedEventArgs e)
    {
        SetLoading(true, "Opening browser for Microsoft sign-in...");
        await App.Auth.BeginOAuthAsync("microsoft");
    }

    private async void FacebookBtn_Click(object sender, RoutedEventArgs e)
    {
        SetLoading(true, "Opening browser for Facebook sign-in...");
        await App.Auth.BeginOAuthAsync("facebook");
    }

    private async void ActivateBtn_Click(object sender, RoutedEventArgs e)
    {
        var email = EmailBox.Text.Trim();
        var key = KeyBox.Text.Trim();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(key))
        {
            ShowStatus("Please enter both email and product key.", isError: true);
            return;
        }

        SetLoading(true, "Validating product key...");
        await App.Auth.ValidateProductKeyAsync(email, key);
    }

    private void OnLoginSuccess(object? sender, UserProfile profile)
    {
        Dispatcher.Invoke(() =>
        {
            SetLoading(false, null);
            ShowStatus($"Welcome, {profile.DisplayName ?? profile.Email}!", isError: false);
            App.Auth.LoginSuccess -= OnLoginSuccess;
            App.Auth.LoginFailed -= OnLoginFailed;
            LoginSucceeded?.Invoke(this, profile);
        });
    }

    private void OnLoginFailed(object? sender, string message)
    {
        Dispatcher.Invoke(() =>
        {
            SetLoading(false, null);
            ShowStatus($"Sign-in failed: {message}", isError: true);
        });
    }

    private void SetLoading(bool loading, string? message)
    {
        LoadingBar.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;
        GoogleBtn.IsEnabled = !loading;
        MicrosoftBtn.IsEnabled = !loading;
        FacebookBtn.IsEnabled = !loading;
        ActivateBtn.IsEnabled = !loading;

        if (message != null)
            ShowStatus(message, isError: false);
    }

    private void ShowStatus(string message, bool isError)
    {
        StatusText.Text = message;
        StatusText.Foreground = isError
            ? (System.Windows.Media.Brush)FindResource("HighlightBrush")
            : (System.Windows.Media.Brush)FindResource("SuccessBrush");
        StatusText.Visibility = Visibility.Visible;
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    protected override void OnClosed(EventArgs e)
    {
        App.Auth.LoginSuccess -= OnLoginSuccess;
        App.Auth.LoginFailed -= OnLoginFailed;
        base.OnClosed(e);
    }
}
