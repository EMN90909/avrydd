using Avryd.Core.Navigation;
using Avryd.Core.Speech;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Speech.Recognition;

namespace Avryd.Core.Commands;

public class VoiceCommandManager : IDisposable
{
    [DllImport("user32.dll")] private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
    [DllImport("user32.dll")] private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    private SpeechRecognitionEngine? _recognizer;
    private readonly SpeechManager _speech;
    private readonly NavigationManager _navigation;
    private bool _disposed;
    private bool _enabled;

    private readonly Dictionary<string, Action<string[]>> _commandHandlers = new();

    public event EventHandler<string>? CommandRecognized;
    public event EventHandler<string>? CommandFailed;

    public bool IsEnabled => _enabled;

    public VoiceCommandManager(SpeechManager speech, NavigationManager navigation)
    {
        _speech = speech;
        _navigation = navigation;
        RegisterBuiltInCommands();
    }

    public void Enable()
    {
        try
        {
            _recognizer = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-US"));
            _recognizer.LoadGrammar(BuildGrammar());
            _recognizer.SpeechRecognized += OnSpeechRecognized;
            _recognizer.SetInputToDefaultAudioDevice();
            _recognizer.RecognizeAsync(RecognizeMode.Multiple);
            _enabled = true;
            _speech.Speak("Voice commands enabled");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Voice command init error: {ex.Message}");
            _enabled = false;
        }
    }

    public void Disable()
    {
        try
        {
            _recognizer?.RecognizeAsyncStop();
            _recognizer?.Dispose();
            _recognizer = null;
        }
        catch { }
        _enabled = false;
        _speech.Speak("Voice commands disabled");
    }

    public void RegisterCommand(string phrase, Action<string[]> handler)
    {
        _commandHandlers[phrase.ToLower()] = handler;
        if (_enabled) RebuildGrammar();
    }

    private void RegisterBuiltInCommands()
    {
        _commandHandlers["open chrome"] = _ => LaunchApp("chrome");
        _commandHandlers["open word"] = _ => LaunchApp("winword");
        _commandHandlers["open excel"] = _ => LaunchApp("excel");
        _commandHandlers["open notepad"] = _ => LaunchApp("notepad");
        _commandHandlers["open settings"] = _ => LaunchApp("ms-settings:");
        _commandHandlers["open file explorer"] = _ => LaunchApp("explorer");
        _commandHandlers["open firefox"] = _ => LaunchApp("firefox");
        _commandHandlers["open edge"] = _ => LaunchApp("msedge");
        _commandHandlers["stop speaking"] = _ => _speech.Stop();
        _commandHandlers["repeat"] = _ => _speech.RepeatLast();
        _commandHandlers["next item"] = _ => _navigation.MoveNext();
        _commandHandlers["previous item"] = _ => _navigation.MovePrevious();
        _commandHandlers["toggle mode"] = _ => _navigation.ToggleMode();
        _commandHandlers["volume up"] = _ => AdjustVolume(2);
        _commandHandlers["volume down"] = _ => AdjustVolume(-2);
        _commandHandlers["mute"] = _ => AdjustVolume(-100);
    }

    private Grammar BuildGrammar()
    {
        var choices = new Choices(_commandHandlers.Keys.ToArray());
        var gb = new GrammarBuilder(choices);

        // Also handle "open [app name]" generically
        var openChoice = new GrammarBuilder("open");
        openChoice.Append(new Choices("chrome", "firefox", "edge", "word", "excel", "notepad",
            "settings", "explorer", "opera", "control panel", "task manager"));

        var combined = new Choices(gb, openChoice);
        return new Grammar(new GrammarBuilder(combined));
    }

    private void RebuildGrammar()
    {
        if (_recognizer == null) return;
        try
        {
            _recognizer.UnloadAllGrammars();
            _recognizer.LoadGrammar(BuildGrammar());
        }
        catch { }
    }

    private void OnSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
    {
        var text = e.Result.Text.ToLower().Trim();
        var confidence = e.Result.Confidence;

        if (confidence < 0.6f) return;

        CommandRecognized?.Invoke(this, text);

        if (_commandHandlers.TryGetValue(text, out var handler))
        {
            handler(Array.Empty<string>());
        }
        else if (text.StartsWith("open "))
        {
            var app = text["open ".Length..].Trim();
            LaunchApp(app);
        }
        else
        {
            CommandFailed?.Invoke(this, $"Unknown command: {text}");
        }
    }

    private void LaunchApp(string app)
    {
        try
        {
            if (app.StartsWith("ms-"))
                Process.Start(new ProcessStartInfo(app) { UseShellExecute = true });
            else
                Process.Start(new ProcessStartInfo(app) { UseShellExecute = true });
            _speech.Speak($"Opening {app}");
        }
        catch
        {
            _speech.Speak($"Could not open {app}");
        }
    }

    private static void AdjustVolume(int delta)
    {
        const int APPCOMMAND_VOLUME_UP = 0x0A;
        const int APPCOMMAND_VOLUME_DOWN = 0x09;
        const int WM_APPCOMMAND = 0x0319;

        var hwnd = FindWindow("Shell_TrayWnd", null!);
        if (hwnd == IntPtr.Zero) return;

        var count = Math.Abs(delta);
        var cmd = delta > 0 ? APPCOMMAND_VOLUME_UP : APPCOMMAND_VOLUME_DOWN;
        for (var i = 0; i < count; i++)
            SendMessage(hwnd, WM_APPCOMMAND, 0, cmd * 0x10000);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Disable();
    }
}
