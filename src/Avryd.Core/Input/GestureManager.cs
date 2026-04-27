using Avryd.Core.Navigation;
using Avryd.Core.Speech;
using Avryd.Core.UIA;

namespace Avryd.Core.Input;

public enum GestureType
{
    SwipeRight,
    SwipeLeft,
    SwipeUp,
    SwipeDown,
    TwoFingerSwipeUp,
    TwoFingerSwipeDown,
    DoubleTap,
    TwoFingerDoubleTap,
    ThreeFingerTap,
    Explore
}

public class GestureEvent
{
    public GestureType Gesture { get; set; }
    public System.Windows.Point Position { get; set; }
    public int FingerCount { get; set; }
}

public class GestureManager : IDisposable
{
    private readonly SpeechManager _speech;
    private readonly NavigationManager _navigation;
    private readonly UIAManager _uia;
    private bool _disposed;

    public event EventHandler<GestureEvent>? GestureDetected;

    public GestureManager(SpeechManager speech, NavigationManager navigation, UIAManager uia)
    {
        _speech = speech;
        _navigation = navigation;
        _uia = uia;
    }

    public void ProcessGesture(GestureEvent gesture)
    {
        GestureDetected?.Invoke(this, gesture);

        switch (gesture.Gesture)
        {
            case GestureType.SwipeRight:
                _navigation.MoveNext();
                break;
            case GestureType.SwipeLeft:
                _navigation.MovePrevious();
                break;
            case GestureType.SwipeUp:
                _navigation.JumpToNext(NavigationTarget.Heading);
                break;
            case GestureType.SwipeDown:
                _navigation.JumpToPrevious(NavigationTarget.Heading);
                break;
            case GestureType.TwoFingerSwipeUp:
                _navigation.JumpToTop();
                break;
            case GestureType.TwoFingerSwipeDown:
                _navigation.JumpToBottom();
                break;
            case GestureType.DoubleTap:
                ActivateFocused();
                break;
            case GestureType.TwoFingerDoubleTap:
                _speech.RepeatLast();
                break;
            case GestureType.ThreeFingerTap:
                _navigation.ToggleMode();
                break;
            case GestureType.Explore:
                ExplorePoint(gesture.Position);
                break;
        }
    }

    private void ActivateFocused()
    {
        var el = _uia.GetFocusedElement();
        if (el == null) { _speech.Speak("Nothing focused"); return; }
        if (_uia.InvokeElement(el))
            _speech.Speak($"Activated {el.Name}");
        else
            _speech.Speak("Cannot activate");
    }

    private void ExplorePoint(System.Windows.Point point)
    {
        var el = _uia.GetElementUnderPoint(point);
        if (el == null) { _speech.Speak("No element"); return; }
        _uia.SetFocus(el);
        _speech.Speak(el.GetSpeechText(), SpeechPriority.Normal, interrupt: true);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}
