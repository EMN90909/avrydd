using System.Windows.Automation;
using System.Diagnostics;

namespace Avryd.Core.UIA;

public class UIAElement
{
    public AutomationElement? Element { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AutomationId { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool IsKeyboardFocusable { get; set; }
    public bool HasKeyboardFocus { get; set; }
    public System.Windows.Rect BoundingRect { get; set; }
    public List<string> SupportedPatterns { get; set; } = new();
    public string State { get; set; } = string.Empty;
    public int ProcessId { get; set; }

    public string GetSpeechText()
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(Name)) parts.Add(Name);
        if (!string.IsNullOrEmpty(Role) && Role != "Unknown") parts.Add(Role);
        if (!string.IsNullOrEmpty(Value)) parts.Add(Value);
        if (!string.IsNullOrEmpty(State)) parts.Add(State);
        if (!string.IsNullOrEmpty(Description)) parts.Add(Description);

        return string.Join(", ", parts.Where(p => !string.IsNullOrEmpty(p)));
    }
}

public class UIAManager : IDisposable
{
    private AutomationElement? _currentFocus;
    private readonly System.Windows.Automation.AutomationFocusChangedEventHandler _focusHandler;
    private readonly System.Windows.Automation.AutomationEventHandler _windowOpenedHandler;
    private readonly System.Windows.Automation.AutomationPropertyChangedEventHandler _propertyChangedHandler;
    private bool _disposed;

    public event EventHandler<UIAElement>? FocusChanged;
    public event EventHandler<UIAElement>? WindowOpened;
    public event EventHandler<UIAElement>? LiveRegionChanged;
    public event EventHandler<string>? NotificationReceived;

    public UIAManager()
    {
        _focusHandler = OnFocusChanged;
        _windowOpenedHandler = OnWindowOpened;
        _propertyChangedHandler = OnPropertyChanged;
    }

    public void Start()
    {
        Automation.AddAutomationFocusChangedEventHandler(_focusHandler);

        Automation.AddAutomationEventHandler(
            WindowPattern.WindowOpenedEvent,
            AutomationElement.RootElement,
            TreeScope.Subtree,
            _windowOpenedHandler);

        // Monitor live regions
        Automation.AddAutomationPropertyChangedEventHandler(
            AutomationElement.RootElement,
            TreeScope.Subtree,
            _propertyChangedHandler,
            AutomationElement.NameProperty,
            ValuePattern.ValueProperty);
    }

    public void Stop()
    {
        try
        {
            Automation.RemoveAutomationFocusChangedEventHandler(_focusHandler);
            Automation.RemoveAllEventHandlers();
        }
        catch { }
    }

    public UIAElement? GetFocusedElement()
    {
        try
        {
            var el = AutomationElement.FocusedElement;
            return el == null ? null : WrapElement(el);
        }
        catch { return null; }
    }

    public UIAElement? GetElementUnderPoint(System.Windows.Point point)
    {
        try
        {
            var el = AutomationElement.FromPoint(point);
            return el == null ? null : WrapElement(el);
        }
        catch { return null; }
    }

    public List<UIAElement> GetChildren(UIAElement parent)
    {
        var result = new List<UIAElement>();
        if (parent.Element == null) return result;

        try
        {
            var walker = TreeWalker.ContentViewWalker;
            var child = walker.GetFirstChild(parent.Element);
            while (child != null)
            {
                result.Add(WrapElement(child));
                child = walker.GetNextSibling(child);
            }
        }
        catch { }
        return result;
    }

    public List<UIAElement> GetFocusableElements(AutomationElement? root = null)
    {
        var result = new List<UIAElement>();
        var searchRoot = root ?? AutomationElement.RootElement;

        try
        {
            var condition = new AndCondition(
                new PropertyCondition(AutomationElement.IsKeyboardFocusableProperty, true),
                new PropertyCondition(AutomationElement.IsEnabledProperty, true));

            var elements = searchRoot.FindAll(TreeScope.Subtree, condition);
            foreach (AutomationElement el in elements)
                result.Add(WrapElement(el));
        }
        catch { }
        return result;
    }

    public List<UIAElement> GetElementsByRole(string controlType, AutomationElement? root = null)
    {
        var result = new List<UIAElement>();
        var searchRoot = root ?? AutomationElement.RootElement;

        try
        {
            ControlType? ct = controlType switch
            {
                "Button" => ControlType.Button,
                "Edit" => ControlType.Edit,
                "ComboBox" => ControlType.ComboBox,
                "CheckBox" => ControlType.CheckBox,
                "RadioButton" => ControlType.RadioButton,
                "List" => ControlType.List,
                "ListItem" => ControlType.ListItem,
                "Menu" => ControlType.Menu,
                "MenuItem" => ControlType.MenuItem,
                "Tab" => ControlType.Tab,
                "TabItem" => ControlType.TabItem,
                "Text" => ControlType.Text,
                "Hyperlink" => ControlType.Hyperlink,
                "Slider" => ControlType.Slider,
                "ProgressBar" => ControlType.ProgressBar,
                "Window" => ControlType.Window,
                "Dialog" => ControlType.Window,
                "TreeItem" => ControlType.TreeItem,
                _ => null
            };

            if (ct == null) return result;
            var cond = new PropertyCondition(AutomationElement.ControlTypeProperty, ct);
            var elements = searchRoot.FindAll(TreeScope.Subtree, cond);
            foreach (AutomationElement el in elements)
                result.Add(WrapElement(el));
        }
        catch { }
        return result;
    }

    public bool InvokeElement(UIAElement element)
    {
        try
        {
            if (element.Element == null) return false;
            if (element.Element.TryGetCurrentPattern(InvokePattern.Pattern, out var pattern))
            {
                ((InvokePattern)pattern).Invoke();
                return true;
            }
            if (element.Element.TryGetCurrentPattern(TogglePattern.Pattern, out var toggle))
            {
                ((TogglePattern)toggle).Toggle();
                return true;
            }
            if (element.Element.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var expand))
            {
                var ec = (ExpandCollapsePattern)expand;
                if (ec.Current.ExpandCollapseState == ExpandCollapseState.Collapsed)
                    ec.Expand();
                else
                    ec.Collapse();
                return true;
            }
        }
        catch { }
        return false;
    }

    public bool SetValue(UIAElement element, string value)
    {
        try
        {
            if (element.Element == null) return false;
            if (element.Element.TryGetCurrentPattern(ValuePattern.Pattern, out var pattern))
            {
                ((ValuePattern)pattern).SetValue(value);
                return true;
            }
        }
        catch { }
        return false;
    }

    public bool SetFocus(UIAElement element)
    {
        try
        {
            element.Element?.SetFocus();
            return true;
        }
        catch { return false; }
    }

    public UIAElement? GetActiveWindow()
    {
        try
        {
            var cond = new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window),
                new PropertyCondition(AutomationElement.IsKeyboardFocusableProperty, true));

            var wins = AutomationElement.RootElement.FindAll(TreeScope.Children, cond);
            foreach (AutomationElement win in wins)
            {
                if (win.TryGetCurrentPattern(WindowPattern.Pattern, out var wp))
                {
                    var windowPat = (WindowPattern)wp;
                    if (windowPat.Current.WindowInteractionState != WindowInteractionState.BlockedByModalWindow)
                        return WrapElement(win);
                }
            }
            return null;
        }
        catch { return null; }
    }

    public static UIAElement WrapElement(AutomationElement el)
    {
        var wrapped = new UIAElement { Element = el };
        try
        {
            wrapped.Name = el.Current.Name ?? string.Empty;
            wrapped.Role = el.Current.ControlType?.ProgrammaticName?.Replace("ControlType.", "") ?? "Unknown";
            wrapped.AutomationId = el.Current.AutomationId ?? string.Empty;
            wrapped.IsEnabled = el.Current.IsEnabled;
            wrapped.IsKeyboardFocusable = el.Current.IsKeyboardFocusable;
            wrapped.HasKeyboardFocus = el.Current.HasKeyboardFocus;
            wrapped.BoundingRect = el.Current.BoundingRectangle;
            wrapped.ProcessId = el.Current.ProcessId;

            // Try to get value
            if (el.TryGetCurrentPattern(ValuePattern.Pattern, out var vp))
                wrapped.Value = ((ValuePattern)vp).Current.Value ?? string.Empty;
            else if (el.TryGetCurrentPattern(RangeValuePattern.Pattern, out var rvp))
                wrapped.Value = ((RangeValuePattern)rvp).Current.Value.ToString("F0");
            else if (el.TryGetCurrentPattern(TextPattern.Pattern, out var tp))
            {
                var ranges = ((TextPattern)tp).GetSelection();
                wrapped.Value = ranges.Length > 0 ? ranges[0].GetText(200) : string.Empty;
            }

            // State
            var stateParts = new List<string>();
            if (el.TryGetCurrentPattern(TogglePattern.Pattern, out var tog))
            {
                var state = ((TogglePattern)tog).Current.ToggleState;
                stateParts.Add(state == ToggleState.On ? "checked" : "unchecked");
            }
            if (el.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var sel))
            {
                if (((SelectionItemPattern)sel).Current.IsSelected) stateParts.Add("selected");
            }
            if (el.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var exc))
            {
                var state = ((ExpandCollapsePattern)exc).Current.ExpandCollapseState;
                stateParts.Add(state == ExpandCollapseState.Expanded ? "expanded" : "collapsed");
            }
            if (!el.Current.IsEnabled) stateParts.Add("disabled");

            wrapped.State = string.Join(", ", stateParts);

            // Description from help text
            wrapped.Description = el.Current.HelpText ?? string.Empty;

            // Supported patterns
            var supportedPatterns = new List<string>();
            if (el.TryGetCurrentPattern(InvokePattern.Pattern, out _)) supportedPatterns.Add("Invoke");
            if (el.TryGetCurrentPattern(ValuePattern.Pattern, out _)) supportedPatterns.Add("Value");
            if (el.TryGetCurrentPattern(TextPattern.Pattern, out _)) supportedPatterns.Add("Text");
            if (el.TryGetCurrentPattern(ScrollPattern.Pattern, out _)) supportedPatterns.Add("Scroll");
            wrapped.SupportedPatterns = supportedPatterns;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"UIAManager WrapElement: {ex.Message}");
        }
        return wrapped;
    }

    private void OnFocusChanged(object sender, AutomationFocusChangedEventArgs e)
    {
        try
        {
            var el = AutomationElement.FocusedElement;
            if (el == null) return;
            _currentFocus = el;
            FocusChanged?.Invoke(this, WrapElement(el));
        }
        catch { }
    }

    private void OnWindowOpened(object sender, AutomationEventArgs e)
    {
        try
        {
            if (sender is AutomationElement el)
                WindowOpened?.Invoke(this, WrapElement(el));
        }
        catch { }
    }

    private void OnPropertyChanged(object sender, AutomationPropertyChangedEventArgs e)
    {
        try
        {
            if (sender is AutomationElement el)
            {
                var wrapped = WrapElement(el);
                var isLiveRegion = false;
                try { isLiveRegion = el.Current.LiveSetting != AutomationLiveSetting.Off; } catch { }

                if (isLiveRegion)
                    LiveRegionChanged?.Invoke(this, wrapped);
            }
        }
        catch { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }
}
