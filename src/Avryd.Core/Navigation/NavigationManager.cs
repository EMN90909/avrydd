using Avryd.Core.Focus;
using Avryd.Core.Speech;
using Avryd.Core.UIA;
using System.Windows.Automation;

namespace Avryd.Core.Navigation;

public enum ReadingMode
{
    Browse,
    Reading,
    Navigation,
    Typing,
    Forms
}

public enum NavigationTarget
{
    Any,
    Heading,
    Button,
    Link,
    Edit,
    Checkbox,
    Table,
    List,
    Landmark
}

public class NavigationManager : IDisposable
{
    private readonly UIAManager _uia;
    private readonly SpeechManager _speech;
    private readonly FocusTracker _focus;

    private ReadingMode _mode = ReadingMode.Browse;
    private List<UIAElement> _virtualBuffer = new();
    private int _bufferIndex = -1;
    private bool _disposed;
    private string _lastTypedChar = string.Empty;

    public event EventHandler<ReadingMode>? ModeChanged;
    public ReadingMode CurrentMode => _mode;

    public NavigationManager(UIAManager uia, SpeechManager speech, FocusTracker focus)
    {
        _uia = uia;
        _speech = speech;
        _focus = focus;
    }

    public void SetMode(ReadingMode mode)
    {
        _mode = mode;
        var name = mode switch
        {
            ReadingMode.Browse => "browse mode",
            ReadingMode.Reading => "reading mode",
            ReadingMode.Navigation => "navigation mode",
            ReadingMode.Typing => "typing mode",
            ReadingMode.Forms => "forms mode",
            _ => mode.ToString()
        };
        _speech.SpeakImmediate(name);
        ModeChanged?.Invoke(this, mode);
    }

    public void ToggleMode()
    {
        var next = (ReadingMode)(((int)_mode + 1) % Enum.GetValues<ReadingMode>().Length);
        SetMode(next);
    }

    public void MoveNext()
    {
        switch (_mode)
        {
            case ReadingMode.Browse: MoveBrowse(1); break;
            case ReadingMode.Reading: ReadNextParagraph(); break;
            case ReadingMode.Navigation: MoveToNextLandmark(); break;
            default: MoveBrowse(1); break;
        }
    }

    public void MovePrevious()
    {
        switch (_mode)
        {
            case ReadingMode.Browse: MoveBrowse(-1); break;
            case ReadingMode.Reading: ReadPreviousParagraph(); break;
            case ReadingMode.Navigation: MoveToPreviousLandmark(); break;
            default: MoveBrowse(-1); break;
        }
    }

    public void JumpToNext(NavigationTarget target)
    {
        var current = _focus.CurrentElement;
        var role = target switch
        {
            NavigationTarget.Button => "Button",
            NavigationTarget.Link => "Hyperlink",
            NavigationTarget.Edit => "Edit",
            NavigationTarget.Checkbox => "CheckBox",
            NavigationTarget.Table => "DataGrid",
            NavigationTarget.List => "List",
            NavigationTarget.Heading => "Text",
            _ => null
        };

        if (role == null) { MoveBrowse(1); return; }

        var elements = _uia.GetElementsByRole(role);
        if (!elements.Any())
        {
            _speech.Speak($"No {target.ToString().ToLower()} found", SpeechPriority.Normal);
            return;
        }

        // Find element after current in DOM order
        UIAElement? next = null;
        if (current?.Element != null)
        {
            var currentBounds = current.BoundingRect;
            next = elements.FirstOrDefault(e =>
                e.BoundingRect.Top > currentBounds.Top ||
                (e.BoundingRect.Top == currentBounds.Top && e.BoundingRect.Left > currentBounds.Left));
        }

        next ??= elements.First();
        _uia.SetFocus(next);
        _speech.Speak(next.GetSpeechText(), SpeechPriority.Normal, interrupt: true);
    }

    public void JumpToPrevious(NavigationTarget target)
    {
        var current = _focus.CurrentElement;
        var role = target switch
        {
            NavigationTarget.Button => "Button",
            NavigationTarget.Link => "Hyperlink",
            NavigationTarget.Edit => "Edit",
            NavigationTarget.Checkbox => "CheckBox",
            _ => null
        };

        if (role == null) { MoveBrowse(-1); return; }

        var elements = _uia.GetElementsByRole(role);
        if (!elements.Any()) { _speech.Speak($"No {target} found"); return; }

        UIAElement? prev = null;
        if (current?.Element != null)
        {
            var currentBounds = current.BoundingRect;
            prev = elements.LastOrDefault(e =>
                e.BoundingRect.Top < currentBounds.Top ||
                (e.BoundingRect.Top == currentBounds.Top && e.BoundingRect.Left < currentBounds.Left));
        }

        prev ??= elements.Last();
        _uia.SetFocus(prev);
        _speech.Speak(prev.GetSpeechText(), SpeechPriority.Normal, interrupt: true);
    }

    public void ReadAll()
    {
        _speech.Stop();
        SetMode(ReadingMode.Reading);
        BuildVirtualBuffer();
        _bufferIndex = 0;
        ReadFromBuffer();
    }

    public void StopReading()
    {
        _speech.Stop();
        SetMode(ReadingMode.Browse);
    }

    public void JumpToTop()
    {
        _bufferIndex = 0;
        BuildVirtualBuffer();
        _speech.SpeakImmediate("Top");
        ReadFromBuffer();
    }

    public void JumpToBottom()
    {
        BuildVirtualBuffer();
        _bufferIndex = _virtualBuffer.Count - 1;
        _speech.SpeakImmediate("Bottom");
        ReadFromBuffer();
    }

    public void AnnounceTypedCharacter(char c)
    {
        if (_mode != ReadingMode.Typing) return;
        _speech.Speak(c == ' ' ? "space" : c.ToString(), SpeechPriority.High, interrupt: true);
        _lastTypedChar = c.ToString();
    }

    public void AnnounceTypedWord(string word)
    {
        if (_mode != ReadingMode.Typing) return;
        _speech.Speak(word, SpeechPriority.Normal, interrupt: true);
    }

    private void MoveBrowse(int direction)
    {
        BuildVirtualBuffer();
        if (!_virtualBuffer.Any()) { _speech.Speak("Empty"); return; }

        _bufferIndex = Math.Clamp(_bufferIndex + direction, 0, _virtualBuffer.Count - 1);
        var el = _virtualBuffer[_bufferIndex];
        _uia.SetFocus(el);

        var text = _focus.BuildAnnouncementText(el);
        _speech.Speak(string.IsNullOrEmpty(text) ? "blank" : text, SpeechPriority.Normal, interrupt: true);
    }

    private void ReadNextParagraph()
    {
        if (_bufferIndex < _virtualBuffer.Count - 1)
        {
            _bufferIndex++;
            ReadFromBuffer();
        }
        else
        {
            _speech.Speak("End of content");
        }
    }

    private void ReadPreviousParagraph()
    {
        if (_bufferIndex > 0)
        {
            _bufferIndex--;
            ReadFromBuffer();
        }
        else
        {
            _speech.Speak("Beginning of content");
        }
    }

    private void MoveToNextLandmark()
    {
        var landmarks = GetLandmarks();
        if (!landmarks.Any()) { _speech.Speak("No landmarks found"); return; }

        var current = _focus.CurrentElement;
        UIAElement? next = landmarks.First();
        if (current != null)
        {
            var idx = landmarks.FindIndex(e => IsSameBounds(e, current));
            if (idx >= 0 && idx < landmarks.Count - 1)
                next = landmarks[idx + 1];
        }

        _uia.SetFocus(next);
        _speech.Speak($"Landmark: {next.Name}", SpeechPriority.Normal, interrupt: true);
    }

    private void MoveToPreviousLandmark()
    {
        var landmarks = GetLandmarks();
        if (!landmarks.Any()) { _speech.Speak("No landmarks found"); return; }

        var current = _focus.CurrentElement;
        var next = landmarks.Last();
        if (current != null)
        {
            var idx = landmarks.FindIndex(e => IsSameBounds(e, current));
            if (idx > 0) next = landmarks[idx - 1];
        }

        _uia.SetFocus(next);
        _speech.Speak($"Landmark: {next.Name}", SpeechPriority.Normal, interrupt: true);
    }

    private List<UIAElement> GetLandmarks()
    {
        var landmarks = new List<UIAElement>();
        try
        {
            var activeWin = _uia.GetActiveWindow();
            if (activeWin?.Element == null) return landmarks;

            var panes = _uia.GetElementsByRole("Pane", activeWin.Element);
            var groups = _uia.GetElementsByRole("Group", activeWin.Element);
            landmarks.AddRange(panes.Where(e => !string.IsNullOrEmpty(e.Name)));
            landmarks.AddRange(groups.Where(e => !string.IsNullOrEmpty(e.Name)));
            landmarks = landmarks.OrderBy(e => e.BoundingRect.Top).ThenBy(e => e.BoundingRect.Left).ToList();
        }
        catch { }
        return landmarks;
    }

    private void BuildVirtualBuffer()
    {
        var activeWin = _uia.GetActiveWindow();
        if (activeWin?.Element == null) { _virtualBuffer = new(); return; }

        _virtualBuffer = _uia.GetFocusableElements(activeWin.Element)
            .OrderBy(e => e.BoundingRect.Top)
            .ThenBy(e => e.BoundingRect.Left)
            .ToList();
    }

    private void ReadFromBuffer()
    {
        if (_bufferIndex < 0 || _bufferIndex >= _virtualBuffer.Count) return;
        var el = _virtualBuffer[_bufferIndex];
        var text = _focus.BuildAnnouncementText(el);
        _speech.Speak(string.IsNullOrEmpty(text) ? "blank" : text, SpeechPriority.Normal);
    }

    private static bool IsSameBounds(UIAElement a, UIAElement b)
    {
        return a.BoundingRect == b.BoundingRect;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}
