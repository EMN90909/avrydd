using Avryd.Core.Focus;
using Avryd.Core.Navigation;
using Avryd.Core.Settings;
using Avryd.Core.Speech;
using Avryd.Core.UIA;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;

namespace Avryd.Service;

public class AvrydWorker : BackgroundService
{
    private readonly ILogger<AvrydWorker> _logger;
    private SettingsManager? _settings;
    private SpeechManager? _speech;
    private UIAManager? _uia;
    private FocusTracker? _focus;
    private NavigationManager? _navigation;

    public AvrydWorker(ILogger<AvrydWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Avryd Service starting");

        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var piperExe = Path.Combine(baseDir, "resources", "piper", "piper.exe");
            var piperModels = Path.Combine(baseDir, "resources", "piper", "voices");

            _settings = new SettingsManager();
            _settings.Load();

            _speech = new SpeechManager(_settings, piperExe, piperModels);
            _speech.Start();

            _uia = new UIAManager();
            _uia.Start();

            _focus = new FocusTracker(_uia, _speech);
            _navigation = new NavigationManager(_uia, _speech, _focus);

            _speech.Speak("Avryd service started", SpeechPriority.Normal);
            _logger.LogInformation("Avryd service running");

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(5000, stoppingToken);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Avryd service error");
        }
        finally
        {
            Cleanup();
        }
    }

    private void Cleanup()
    {
        _focus?.Dispose();
        _navigation?.Dispose();
        _uia?.Dispose();
        _speech?.Dispose();
        _logger.LogInformation("Avryd service stopped");
    }

    public override void Dispose()
    {
        Cleanup();
        base.Dispose();
    }
}
