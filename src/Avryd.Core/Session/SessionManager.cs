using Avryd.Core.Models;
using Avryd.Core.Settings;
using Newtonsoft.Json;

namespace Avryd.Core.Session;

public class SessionManager : IDisposable
{
    private readonly SettingsManager _settings;
    private readonly string _sessionsFile;
    private DateTime _sessionStart;
    private bool _sessionActive;
    private List<SessionRecord> _sessions = new();
    private bool _disposed;

    public TimeSpan CurrentSessionDuration =>
        _sessionActive ? DateTime.UtcNow - _sessionStart : TimeSpan.Zero;

    public int TotalSessionCount => _sessions.Count;

    public double TotalUsageMinutes =>
        _sessions.Sum(s => s.DurationMinutes) + CurrentSessionDuration.TotalMinutes;

    public event EventHandler<TimeSpan>? SessionTick;

    private readonly System.Timers.Timer _ticker;

    public SessionManager(SettingsManager settings)
    {
        _settings = settings;
        _sessionsFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Avryd", "sessions.json");

        LoadSessions();

        _ticker = new System.Timers.Timer(60_000); // tick every minute
        _ticker.Elapsed += (s, e) => SessionTick?.Invoke(this, CurrentSessionDuration);
    }

    public void StartSession()
    {
        _sessionStart = DateTime.UtcNow;
        _sessionActive = true;
        _ticker.Start();
    }

    public void EndSession()
    {
        if (!_sessionActive) return;
        _sessionActive = false;
        _ticker.Stop();

        var record = new SessionRecord
        {
            StartTime = _sessionStart,
            EndTime = DateTime.UtcNow,
            DurationMinutes = (DateTime.UtcNow - _sessionStart).TotalMinutes
        };
        _sessions.Add(record);

        // Keep last 90 sessions
        if (_sessions.Count > 90)
            _sessions = _sessions.TakeLast(90).ToList();

        SaveSessions();

        // Update profile stats
        if (_settings.Profile != null)
        {
            _settings.Profile.TotalSessions = _sessions.Count;
            _settings.Profile.TotalUsageMinutes = TotalUsageMinutes;
            _settings.SaveProfile(_settings.Profile);
        }
    }

    public string GetUsageSummary()
    {
        var total = TimeSpan.FromMinutes(TotalUsageMinutes);
        return $"Sessions: {TotalSessionCount}, Total time: {(int)total.TotalHours}h {total.Minutes}m";
    }

    public List<SessionRecord> GetRecentSessions(int count = 10)
        => _sessions.TakeLast(count).ToList();

    private void LoadSessions()
    {
        try
        {
            if (File.Exists(_sessionsFile))
            {
                var json = File.ReadAllText(_sessionsFile);
                _sessions = JsonConvert.DeserializeObject<List<SessionRecord>>(json) ?? new();
            }
        }
        catch { _sessions = new(); }
    }

    private void SaveSessions()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_sessionsFile)!);
            File.WriteAllText(_sessionsFile, JsonConvert.SerializeObject(_sessions, Formatting.Indented));
        }
        catch { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        EndSession();
        _ticker.Dispose();
    }
}
