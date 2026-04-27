using Avryd.Core.OCR;
using Avryd.Core.Speech;
using Avryd.Core.UIA;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Automation;

namespace Avryd.Core.Fallback;

public class FallbackReader : IDisposable
{
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);
    [DllImport("user32.dll")] private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);
    [DllImport("oleacc.dll")] private static extern int AccessibleObjectFromWindow(IntPtr hwnd, uint id, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppvObject);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    private static readonly Guid IID_IAccessible = new("618736E0-3C3D-11CF-810C-00AA00389B71");
    private const uint OBJID_CLIENT = 0xFFFFFFFC;
    private const uint OBJID_WINDOW = 0x00000000;

    private readonly OCRManager _ocr;
    private readonly SpeechManager _speech;
    private readonly UIAManager _uia;
    private readonly VirtualUIATree _virtualTree;
    private bool _disposed;

    private readonly System.Timers.Timer _pollTimer;
    private IntPtr _lastWindow = IntPtr.Zero;
    private string _lastWindowTitle = string.Empty;
    private bool _isActivelyUsed;
    private DateTime _lastInteraction = DateTime.UtcNow;

    public event EventHandler<UIAElement>? FallbackElementFocused;

    public FallbackReader(OCRManager ocr, SpeechManager speech, UIAManager uia)
    {
        _ocr = ocr;
        _speech = speech;
        _uia = uia;
        _virtualTree = new VirtualUIATree();

        _pollTimer = new System.Timers.Timer(500);
        _pollTimer.Elapsed += OnPollTick;
    }

    public void Start()
    {
        _pollTimer.Start();
    }

    public void Stop()
    {
        _pollTimer.Stop();
    }

    public void MarkInteraction()
    {
        _lastInteraction = DateTime.UtcNow;
        _isActivelyUsed = true;
        // Reduce poll interval when active
        _pollTimer.Interval = 300;
    }

    private async void OnPollTick(object? sender, System.Timers.ElapsedEventArgs e)
    {
        // Adaptive: slow down when idle
        var idleTime = DateTime.UtcNow - _lastInteraction;
        if (idleTime.TotalSeconds > 5)
        {
            _pollTimer.Interval = 2000;
            _isActivelyUsed = false;
        }

        var hwnd = GetForegroundWindow();
        if (hwnd == _lastWindow && !_isActivelyUsed) return;

        // Check if UIA provides a valid tree first
        if (HasValidUIATree(hwnd))
        {
            _lastWindow = hwnd;
            return;
        }

        // Try IAccessible (legacy COM)
        var accessible = TryGetIAccessible(hwnd);
        if (accessible != null)
        {
            _lastWindow = hwnd;
            return;
        }

        // Fall back to OCR
        await RunOcrFallbackAsync(hwnd);
        _lastWindow = hwnd;
    }

    private static bool HasValidUIATree(IntPtr hwnd)
    {
        try
        {
            var el = AutomationElement.FromHandle(hwnd);
            if (el == null) return false;

            var condition = new PropertyCondition(AutomationElement.IsKeyboardFocusableProperty, true);
            var children = el.FindAll(TreeScope.Subtree, condition);
            return children.Count > 0;
        }
        catch { return false; }
    }

    private dynamic? TryGetIAccessible(IntPtr hwnd)
    {
        try
        {
            var guid = IID_IAccessible;
            var hr = AccessibleObjectFromWindow(hwnd, OBJID_CLIENT, ref guid, out var accObj);
            if (hr == 0 && accObj != null)
            {
                // Successfully got IAccessible - the UIA bridge handles the rest
                return accObj;
            }
        }
        catch { }
        return null;
    }

    private async Task RunOcrFallbackAsync(IntPtr hwnd)
    {
        try
        {
            if (!_ocr.IsAvailable) return;
            if (!GetWindowRect(hwnd, out var rect)) return;

            var bounds = new System.Windows.Rect(
                rect.Left, rect.Top,
                rect.Right - rect.Left,
                rect.Bottom - rect.Top);

            if (bounds.Width < 10 || bounds.Height < 10) return;

            var result = await _ocr.CaptureAndRecognizeBoundsAsync(bounds);
            if (!result.Success) return;

            _virtualTree.BuildFromOcr(result, bounds);

            // Announce first element from virtual tree
            var first = _virtualTree.GetNext();
            if (first != null)
                FallbackElementFocused?.Invoke(this, first);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"OCR fallback error: {ex.Message}");
        }
    }

    public UIAElement? GetNextFallbackElement() => _virtualTree.GetNext();
    public UIAElement? GetPreviousFallbackElement() => _virtualTree.GetPrevious();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _pollTimer.Stop();
        _pollTimer.Dispose();
    }
}
