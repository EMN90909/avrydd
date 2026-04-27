namespace Avryd.Core.Plugins;

public interface IPlugin
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    string Version { get; }
    string Author { get; }
    string[] SupportedLanguages { get; }

    void Initialize(IPluginHost host);
    void Shutdown();
}

public interface IPluginHost
{
    void Speak(string text);
    void RegisterCommand(string command, Action<string[]> handler);
    void UnregisterCommand(string command);
    void Log(string message, LogLevel level = LogLevel.Info);
}

public enum LogLevel { Debug, Info, Warning, Error }

public class PluginInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public bool IsInstalled { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
}
