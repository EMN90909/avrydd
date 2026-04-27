using System.Runtime.InteropServices;
using Avryd.Core.Navigation;
using Avryd.Core.Speech;

namespace Avryd.Core.Input;

public class KeyboardManager : IDisposable
{
    [DllImport("user32.dll")] private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
    [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
    [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll")] private static extern IntPtr GetModuleHandle(string? lpModuleName);

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;

    private IntPtr _hookId = IntPtr.Zero;
    private readonly LowLevelKeyboardProc _proc;
    private readonly SpeechManager _speech;
    private readonly NavigationManager _navigation;
    private bool _disposed;

    public event EventHandler<KeyEventArgs>? KeyPressed;

    public KeyboardManager(SpeechManager speech, NavigationManager navigation)
    {
        _speech = speech;
        _navigation = navigation;
        _proc = HookCallback;
    }

    public void Install()
    {
        using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
    }

    public void Uninstall()
    {
        if (_hookId != IntPtr.Zero) UnhookWindowsHookEx(_hookId);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN))
        {
            var vkCode = Marshal.ReadInt32(lParam);
            var key = (System.Windows.Forms.Keys)vkCode;
            var ctrl = (System.Windows.Forms.Control.ModifierKeys & System.Windows.Forms.Keys.Control) != 0;
            var alt = (System.Windows.Forms.Control.ModifierKeys & System.Windows.Forms.Keys.Alt) != 0;
            var shift = (System.Windows.Forms.Control.ModifierKeys & System.Windows.Forms.Keys.Shift) != 0;

            var args = new KeyEventArgs(key, ctrl, alt, shift);
            KeyPressed?.Invoke(this, args);

            if (HandleAvrydShortcut(key, ctrl, alt, shift))
                return new IntPtr(1); // Suppress key
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private bool HandleAvrydShortcut(System.Windows.Forms.Keys key, bool ctrl, bool alt, bool shift)
    {
        // Ctrl+Alt+S = Stop speech
        if (ctrl && alt && key == System.Windows.Forms.Keys.S)
        {
            _speech.Stop();
            return true;
        }
        // Ctrl+Alt+R = Read current item
        if (ctrl && alt && key == System.Windows.Forms.Keys.R)
        {
            // Handled by FocusTracker
            return false;
        }
        // Ctrl+Alt+Right = Next item
        if (ctrl && alt && key == System.Windows.Forms.Keys.Right)
        {
            _navigation.MoveNext();
            return true;
        }
        // Ctrl+Alt+Left = Previous item
        if (ctrl && alt && key == System.Windows.Forms.Keys.Left)
        {
            _navigation.MovePrevious();
            return true;
        }
        // Ctrl+Alt+M = Toggle mode
        if (ctrl && alt && key == System.Windows.Forms.Keys.M)
        {
            _navigation.ToggleMode();
            return true;
        }
        // Ctrl+Alt+H = Jump to heading
        if (ctrl && alt && key == System.Windows.Forms.Keys.H)
        {
            _navigation.JumpToNext(NavigationTarget.Heading);
            return true;
        }
        // Ctrl+Alt+B = Jump to button
        if (ctrl && alt && key == System.Windows.Forms.Keys.B)
        {
            _navigation.JumpToNext(NavigationTarget.Button);
            return true;
        }
        // Ctrl+Alt+L = Jump to link
        if (ctrl && alt && key == System.Windows.Forms.Keys.L)
        {
            _navigation.JumpToNext(NavigationTarget.Link);
            return true;
        }
        // Ctrl+Alt+E = Jump to edit field
        if (ctrl && alt && key == System.Windows.Forms.Keys.E)
        {
            _navigation.JumpToNext(NavigationTarget.Edit);
            return true;
        }
        // Ctrl+Alt+Home = Jump to top
        if (ctrl && alt && key == System.Windows.Forms.Keys.Home)
        {
            _navigation.JumpToTop();
            return true;
        }
        // Ctrl+Alt+End = Jump to bottom
        if (ctrl && alt && key == System.Windows.Forms.Keys.End)
        {
            _navigation.JumpToBottom();
            return true;
        }
        // Ctrl+Alt+A = Read all
        if (ctrl && alt && key == System.Windows.Forms.Keys.A)
        {
            _navigation.ReadAll();
            return true;
        }
        // Ctrl+Alt+P = Repeat last
        if (ctrl && alt && key == System.Windows.Forms.Keys.P)
        {
            _speech.RepeatLast();
            return true;
        }

        return false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Uninstall();
    }
}

public class KeyEventArgs : EventArgs
{
    public System.Windows.Forms.Keys Key { get; }
    public bool Ctrl { get; }
    public bool Alt { get; }
    public bool Shift { get; }

    public KeyEventArgs(System.Windows.Forms.Keys key, bool ctrl, bool alt, bool shift)
    {
        Key = key; Ctrl = ctrl; Alt = alt; Shift = shift;
    }
}
