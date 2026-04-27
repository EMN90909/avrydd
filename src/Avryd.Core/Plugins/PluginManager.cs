using Avryd.Core.Speech;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;

namespace Avryd.Core.Plugins;

public class PluginHost : IPluginHost
{
    private readonly SpeechManager _speech;
    private readonly Dictionary<string, Action<string[]>> _commands;

    public PluginHost(SpeechManager speech, Dictionary<string, Action<string[]>> commands)
    {
        _speech = speech;
        _commands = commands;
    }

    public void Speak(string text) => _speech.Speak(text);
    public void RegisterCommand(string command, Action<string[]> handler) => _commands[command.ToLower()] = handler;
    public void UnregisterCommand(string command) => _commands.Remove(command.ToLower());
    public void Log(string message, LogLevel level = LogLevel.Info) => Debug.WriteLine($"[Plugin][{level}] {message}");
}

public class PluginManager : IDisposable
{
    private const string PluginRepoUrl = "https://api.github.com/repos/avryd/avryd/contents/plugins";
    private readonly string _pluginsDir;
    private readonly SpeechManager _speech;
    private readonly HttpClient _http;
    private readonly Dictionary<string, IPlugin> _loaded = new();
    private readonly Dictionary<string, Action<string[]>> _commands = new();
    private bool _disposed;

    public event EventHandler<PluginInfo>? PluginInstalled;
    public event EventHandler<string>? PluginRemoved;

    public IReadOnlyList<IPlugin> LoadedPlugins => _loaded.Values.ToList();

    public PluginManager(SpeechManager speech, string pluginsDir)
    {
        _speech = speech;
        _pluginsDir = pluginsDir;
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("User-Agent", "Avryd/1.0");
        Directory.CreateDirectory(_pluginsDir);
    }

    public void LoadInstalled()
    {
        foreach (var dll in Directory.GetFiles(_pluginsDir, "*.dll"))
        {
            try { LoadPlugin(dll); }
            catch (Exception ex) { Debug.WriteLine($"Plugin load error ({dll}): {ex.Message}"); }
        }
    }

    private void LoadPlugin(string dllPath)
    {
        var asm = Assembly.LoadFrom(dllPath);
        foreach (var type in asm.GetExportedTypes())
        {
            if (!typeof(IPlugin).IsAssignableFrom(type) || type.IsInterface) continue;

            var plugin = (IPlugin)Activator.CreateInstance(type)!;
            var host = new PluginHost(_speech, _commands);
            plugin.Initialize(host);
            _loaded[plugin.Id] = plugin;
            Debug.WriteLine($"Loaded plugin: {plugin.Name} v{plugin.Version}");
        }
    }

    public async Task<List<PluginInfo>> FetchAvailablePluginsAsync()
    {
        var plugins = new List<PluginInfo>();
        try
        {
            var json = await _http.GetStringAsync(PluginRepoUrl);
            var items = JsonConvert.DeserializeObject<List<GithubContent>>(json);
            if (items == null) return plugins;

            foreach (var item in items.Where(i => i.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)))
            {
                plugins.Add(new PluginInfo
                {
                    Id = Path.GetFileNameWithoutExtension(item.Name),
                    Name = Path.GetFileNameWithoutExtension(item.Name),
                    FileName = item.Name,
                    DownloadUrl = item.DownloadUrl ?? string.Empty,
                    IsInstalled = File.Exists(Path.Combine(_pluginsDir, item.Name))
                });
            }
        }
        catch (Exception ex) { Debug.WriteLine($"Fetch plugins error: {ex.Message}"); }
        return plugins;
    }

    public async Task<bool> InstallPluginAsync(PluginInfo info)
    {
        try
        {
            if (string.IsNullOrEmpty(info.DownloadUrl)) return false;
            var bytes = await _http.GetByteArrayAsync(info.DownloadUrl);
            var dest = Path.Combine(_pluginsDir, info.FileName);
            await File.WriteAllBytesAsync(dest, bytes);
            LoadPlugin(dest);
            info.IsInstalled = true;
            PluginInstalled?.Invoke(this, info);
            return true;
        }
        catch { return false; }
    }

    public void RemovePlugin(string pluginId)
    {
        if (_loaded.TryGetValue(pluginId, out var plugin))
        {
            plugin.Shutdown();
            _loaded.Remove(pluginId);
        }
        var file = Directory.GetFiles(_pluginsDir, "*.dll")
            .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == pluginId);
        if (file != null) File.Delete(file);
        PluginRemoved?.Invoke(this, pluginId);
    }

    public bool ExecuteCommand(string command, string[] args)
    {
        var key = command.ToLower().Trim();
        if (_commands.TryGetValue(key, out var handler))
        {
            handler(args);
            return true;
        }
        return false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var plugin in _loaded.Values)
            try { plugin.Shutdown(); } catch { }
        _http.Dispose();
    }

    private class GithubContent
    {
        [JsonProperty("name")] public string Name { get; set; } = string.Empty;
        [JsonProperty("download_url")] public string? DownloadUrl { get; set; }
        [JsonProperty("type")] public string Type { get; set; } = string.Empty;
    }
}
