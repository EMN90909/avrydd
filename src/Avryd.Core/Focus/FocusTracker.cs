using Avryd.Core.Speech;
using Avryd.Core.UIA;
using System.Windows.Automation;

namespace Avryd.Core.Focus;

public class FocusTracker : IDisposable
{
    private readonly UIAManager _uia;
    private readonly SpeechManager _speech;
    private UIAElement? _currentElement;
    private UIAElement? _previousElement;
    private bool _disposed;
    private readonly Queue<UIAElement> _history = new(20);

    public event EventHandler<UIAElement>? FocusedElementChanged;
    public UIAElement? CurrentElement => _currentElement;

    public FocusTracker(UIAManager uia, SpeechManager speech)
    {
        _uia = uia;
        _speech = speech;
        _uia.FocusChanged += OnFocusChanged;
        _uia.WindowOpened += OnWindowOpened;
        _uia.LiveRegionChanged += OnLiveRegionChanged;
    }

    private void OnFocusChanged(object? sender, UIAElement element)
    {
        if (IsSameElement(element, _currentElement)) return;

        _previousElement = _currentElement;
        _currentElement = element;

        if (_history.Count >= 20) _history.Dequeue();
        _history.Enqueue(element);

        FocusedElementChanged?.Invoke(this, element);
        AnnounceElement(element);
    }

    private void OnWindowOpened(object? sender, UIAElement window)
    {
        var name = string.IsNullOrEmpty(window.Name) ? "window" : window.Name;
        _speech.Speak($"Window opened: {name}", SpeechPriority.High, interrupt: true);
    }

    private void OnLiveRegionChanged(object? sender, UIAElement element)
    {
        if (!string.IsNullOrEmpty(element.Name))
            _speech.Speak(element.Name, SpeechPriority.Normal);
    }

    private void AnnounceElement(UIAElement element)
    {
        if (element == null) return;

        var speech = BuildAnnouncementText(element);
        if (!string.IsNullOrEmpty(speech))
            _speech.Speak(speech, SpeechPriority.Normal, interrupt: true);
    }

    public string BuildAnnouncementText(UIAElement element)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(element.Name))
            parts.Add(element.Name);

        var friendlyRole = GetFriendlyRole(element.Role);
        if (!string.IsNullOrEmpty(friendlyRole))
            parts.Add(friendlyRole);

        if (!string.IsNullOrEmpty(element.Value))
            parts.Add(element.Value);

        if (!string.IsNullOrEmpty(element.State))
            parts.Add(element.State);

        if (!string.IsNullOrEmpty(element.Description))
            parts.Add(element.Description);

        return string.Join(", ", parts);
    }

    private static string GetFriendlyRole(string role)
    {
        return role switch
        {
            "Button" => "button",
            "Edit" => "text field",
            "ComboBox" => "combo box",
            "CheckBox" => "checkbox",
            "RadioButton" => "radio button",
            "List" => "list",
            "ListItem" => "list item",
            "Menu" => "menu",
            "MenuItem" => "menu item",
            "TabItem" => "tab",
            "Text" => string.Empty,
            "Hyperlink" => "link",
            "Slider" => "slider",
            "ProgressBar" => "progress bar",
            "Window" => "window",
            "TreeItem" => "tree item",
            "DataItem" => "data item",
            "DataGrid" => "table",
            "HeaderItem" => "column header",
            "Document" => "document",
            "ScrollBar" => "scroll bar",
            "ToolBar" => "toolbar",
            "StatusBar" => "status bar",
            _ => role.ToLower()
        };
    }

    public UIAElement? GetPreviousFocus() => _previousElement;

    public void ReadCurrentElement()
    {
        if (_currentElement != null)
            AnnounceElement(_currentElement);
        else
            _speech.Speak("No element focused", SpeechPriority.Normal);
    }

    private static bool IsSameElement(UIAElement a, UIAElement? b)
    {
        if (b == null) return false;
        if (a.Element == null || b.Element == null) return false;
        try
        {
            return AutomationElement.Equals(a.Element, b.Element);
        }
        catch { return false; }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _uia.FocusChanged -= OnFocusChanged;
        _uia.WindowOpened -= OnWindowOpened;
        _uia.LiveRegionChanged -= OnLiveRegionChanged;
    }
}
