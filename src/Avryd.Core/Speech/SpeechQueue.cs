namespace Avryd.Core.Speech;

public enum SpeechPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}

public class SpeechItem
{
    public string Text { get; set; } = string.Empty;
    public SpeechPriority Priority { get; set; } = SpeechPriority.Normal;
    public bool Interruptible { get; set; } = true;
    public DateTime Queued { get; set; } = DateTime.UtcNow;
}

public class SpeechQueue
{
    private readonly object _lock = new();
    private readonly List<SpeechItem> _queue = new();
    private SpeechItem? _current;

    public event EventHandler<SpeechItem>? ItemDequeued;
    public event EventHandler? QueueEmpty;

    public bool IsEmpty
    {
        get { lock (_lock) return _queue.Count == 0; }
    }

    public SpeechItem? Current => _current;

    public void Enqueue(string text, SpeechPriority priority = SpeechPriority.Normal, bool interruptible = true)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        var item = new SpeechItem { Text = text, Priority = priority, Interruptible = interruptible };

        lock (_lock)
        {
            if (priority == SpeechPriority.Urgent)
            {
                _queue.RemoveAll(i => i.Interruptible);
                _queue.Insert(0, item);
            }
            else
            {
                _queue.Add(item);
                _queue.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            }
        }
    }

    public SpeechItem? Dequeue()
    {
        lock (_lock)
        {
            if (_queue.Count == 0) { QueueEmpty?.Invoke(this, EventArgs.Empty); return null; }
            _current = _queue[0];
            _queue.RemoveAt(0);
            ItemDequeued?.Invoke(this, _current);
            return _current;
        }
    }

    public void Clear(bool keepUrgent = false)
    {
        lock (_lock)
        {
            if (keepUrgent)
                _queue.RemoveAll(i => i.Priority != SpeechPriority.Urgent);
            else
                _queue.Clear();
        }
    }

    public void ClearCurrent()
    {
        _current = null;
    }

    public int Count
    {
        get { lock (_lock) return _queue.Count; }
    }
}
