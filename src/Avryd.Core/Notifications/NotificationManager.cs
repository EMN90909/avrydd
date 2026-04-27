using Avryd.Core.Speech;
using System.Windows.Automation;

namespace Avryd.Core.Notifications;

public enum NotificationPriority { Low, Normal, High, Urgent }

public class AvrydNotification
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationPriority Priority { get; set; }
    public DateTime Received { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = string.Empty;
}

public class NotificationManager : IDisposable
{
    private readonly SpeechManager _speech;
    private readonly Queue<AvrydNotification> _queue = new();
    private readonly System.Timers.Timer _processTimer;
    private bool _disposed;
    private DateTime _lastSpoken = DateTime.MinValue;
    private const int MinIntervalMs = 2000;

    public event EventHandler<AvrydNotification>? NotificationArrived;

    public NotificationManager(SpeechManager speech)
    {
        _speech = speech;
        _processTimer = new System.Timers.Timer(500);
        _processTimer.Elapsed += ProcessQueue;
        _processTimer.Start();

        // Hook into Windows notification system via UIA
        HookNotifications();
    }

    private void HookNotifications()
    {
        try
        {
            Automation.AddAutomationEventHandler(
                AutomationElement.AutomationPropertyChangedEvent,
                AutomationElement.RootElement,
                TreeScope.Subtree,
                OnUiaNotification,
                AutomationElement.NameProperty);
        }
        catch { }
    }

    private void OnUiaNotification(object sender, AutomationEventArgs e)
    {
        try
        {
            if (sender is not AutomationElement el) return;

            var liveRegion = AutomationLiveSetting.Off;
            try { liveRegion = el.Current.LiveSetting; } catch { return; }

            if (liveRegion == AutomationLiveSetting.Off) return;

            var name = el.Current.Name;
            if (string.IsNullOrWhiteSpace(name)) return;

            var priority = liveRegion == AutomationLiveSetting.Assertive
                ? NotificationPriority.High
                : NotificationPriority.Normal;

            Enqueue(new AvrydNotification
            {
                Title = string.Empty,
                Body = name,
                Priority = priority,
                Source = "LiveRegion"
            });
        }
        catch { }
    }

    public void Enqueue(AvrydNotification notification)
    {
        lock (_queue)
        {
            if (notification.Priority == NotificationPriority.Urgent)
            {
                // Clear lower priority
                _queue.Clear();
                _queue.Enqueue(notification);
            }
            else if (_queue.Count < 20)
            {
                _queue.Enqueue(notification);
            }
        }

        NotificationArrived?.Invoke(this, notification);
    }

    public void AnnounceNotification(string title, string body, NotificationPriority priority = NotificationPriority.Normal)
    {
        Enqueue(new AvrydNotification { Title = title, Body = body, Priority = priority, Source = "App" });
    }

    private void ProcessQueue(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if ((DateTime.UtcNow - _lastSpoken).TotalMilliseconds < MinIntervalMs) return;

        AvrydNotification? notification = null;
        lock (_queue)
        {
            if (_queue.Count > 0)
                notification = _queue.Dequeue();
        }

        if (notification == null) return;

        var text = string.IsNullOrEmpty(notification.Title)
            ? notification.Body
            : $"{notification.Title}: {notification.Body}";

        var priority = notification.Priority switch
        {
            NotificationPriority.Urgent => SpeechPriority.Urgent,
            NotificationPriority.High => SpeechPriority.High,
            _ => SpeechPriority.Normal
        };

        _speech.Speak(text, priority, interrupt: notification.Priority >= NotificationPriority.High);
        _lastSpoken = DateTime.UtcNow;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _processTimer.Stop();
        _processTimer.Dispose();
        try { Automation.RemoveAllEventHandlers(); } catch { }
    }
}
