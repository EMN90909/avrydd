using Avryd.Core.Models;
using Avryd.Core.Settings;
using System.Diagnostics;
using System.Media;
using System.Runtime.InteropServices;

namespace Avryd.Core.Speech;

public class SpeechManager : IDisposable
{
    private readonly PiperTTS _piper;
    private readonly SpeechQueue _queue;
    private readonly SettingsManager _settingsManager;
    private CancellationTokenSource _cts = new();
    private Task? _processingTask;
    private bool _paused;
    private SpeechItem? _lastSpoken;
    private bool _disposed;

    public event EventHandler<string>? SpeechStarted;
    public event EventHandler<string>? SpeechFinished;
    public event EventHandler? SpeechStopped;

    public bool IsSpeaking { get; private set; }
    public bool IsPaused => _paused;

    public SpeechManager(SettingsManager settings, string piperExe, string piperModelPath)
    {
        _settingsManager = settings;
        _piper = new PiperTTS(piperExe, piperModelPath);
        _queue = new SpeechQueue();
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _processingTask = Task.Run(ProcessQueueAsync, _cts.Token);
    }

    public void Speak(string text, SpeechPriority priority = SpeechPriority.Normal, bool interrupt = false)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        var processed = ProcessText(text);
        if (interrupt) Stop(clearQueue: true);
        _queue.Enqueue(processed, priority);
    }

    public void SpeakImmediate(string text)
    {
        Speak(text, SpeechPriority.Urgent, interrupt: true);
    }

    public void Stop(bool clearQueue = true)
    {
        if (clearQueue) _queue.Clear();
        _piper.StopCurrent();
        IsSpeaking = false;
        SpeechStopped?.Invoke(this, EventArgs.Empty);
    }

    public void Pause()
    {
        _paused = true;
        _piper.StopCurrent();
    }

    public void Resume()
    {
        _paused = false;
    }

    public void RepeatLast()
    {
        if (_lastSpoken != null)
            Speak(_lastSpoken.Text, SpeechPriority.High, interrupt: true);
    }

    public List<string> GetAvailableVoices() => _piper.GetAvailableVoices();

    private async Task ProcessQueueAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                if (_paused) { await Task.Delay(100, _cts.Token); continue; }

                var item = _queue.Dequeue();
                if (item == null) { await Task.Delay(50, _cts.Token); continue; }

                _lastSpoken = item;
                IsSpeaking = true;
                SpeechStarted?.Invoke(this, item.Text);

                var s = _settingsManager.Settings;
                await _piper.SpeakAsync(item.Text, s.VoiceId, s.Rate, s.Volume, _cts.Token);

                IsSpeaking = false;
                _queue.ClearCurrent();
                SpeechFinished?.Invoke(this, item.Text);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Debug.WriteLine($"Speech error: {ex.Message}");
                IsSpeaking = false;
                await Task.Delay(100);
            }
        }
    }

    private string ProcessText(string raw)
    {
        var s = _settingsManager.Settings;

        // Expand abbreviations, numbers, etc.
        var text = raw.Trim();

        // Handle punctuation level
        if (s.PunctuationLevel == "none")
            text = RemovePunctuation(text);
        else if (s.PunctuationLevel == "some")
            text = ReducePunctuation(text);

        // Expand common abbreviations
        text = ExpandAbbreviations(text);

        return text;
    }

    private static string RemovePunctuation(string text)
    {
        return new string(text.Where(c => !char.IsPunctuation(c) || c == '.' || c == ',' || c == '?').ToArray());
    }

    private static string ReducePunctuation(string text)
    {
        return text.Replace("...", " ellipsis ").Replace("--", " dash ");
    }

    private static string ExpandAbbreviations(string text)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "btn", "button" }, { "chk", "checkbox" }, { "txt", "text" },
            { "dlg", "dialog" }, { "lbl", "label" }, { "msg", "message" },
            { "err", "error" }, { "ok", "okay" }, { "pg", "page" }
        };

        var words = text.Split(' ');
        for (var i = 0; i < words.Length; i++)
            if (map.TryGetValue(words[i].ToLower(), out var expanded))
                words[i] = expanded;

        return string.Join(' ', words);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts.Cancel();
        _processingTask?.Wait(2000);
        _piper.Dispose();
        _cts.Dispose();
    }
}
