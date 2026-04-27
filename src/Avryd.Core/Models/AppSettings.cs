using Newtonsoft.Json;

namespace Avryd.Core.Models;

public class AppSettings
{
    [JsonProperty("voice_id")]
    public string? VoiceId { get; set; } = "en_US-lessac-medium";

    [JsonProperty("rate")]
    public int Rate { get; set; } = 175;

    [JsonProperty("pitch")]
    public double Pitch { get; set; } = 1.0;

    [JsonProperty("volume")]
    public double Volume { get; set; } = 1.0;

    [JsonProperty("verbosity")]
    public string Verbosity { get; set; } = "standard";

    [JsonProperty("punctuation_level")]
    public string PunctuationLevel { get; set; } = "some";

    [JsonProperty("typing_echo")]
    public string TypingEcho { get; set; } = "characters";

    [JsonProperty("reading_mode")]
    public string ReadingMode { get; set; } = "browse";

    [JsonProperty("language")]
    public string Language { get; set; } = "en-US";

    [JsonProperty("speak_passwords")]
    public bool SpeakPasswords { get; set; } = false;

    [JsonProperty("speak_tooltips")]
    public bool SpeakTooltips { get; set; } = true;

    [JsonProperty("speak_notifications")]
    public bool SpeakNotifications { get; set; } = true;

    [JsonProperty("haptic_feedback")]
    public bool HapticFeedback { get; set; } = true;

    [JsonProperty("sound_effects")]
    public bool SoundEffects { get; set; } = true;

    [JsonProperty("gesture_sensitivity")]
    public double GestureSensitivity { get; set; } = 1.0;

    [JsonProperty("hotkeys_enabled")]
    public bool HotkeysEnabled { get; set; } = true;

    [JsonProperty("hotkey_stop_speech")]
    public string HotkeyStopSpeech { get; set; } = "Ctrl+Alt+S";

    [JsonProperty("hotkey_read_item")]
    public string HotkeyReadItem { get; set; } = "Ctrl+Alt+R";

    [JsonProperty("hotkey_next_item")]
    public string HotkeyNextItem { get; set; } = "Ctrl+Alt+Right";

    [JsonProperty("hotkey_prev_item")]
    public string HotkeyPrevItem { get; set; } = "Ctrl+Alt+Left";

    [JsonProperty("hotkey_toggle_mode")]
    public string HotkeyToggleMode { get; set; } = "Ctrl+Alt+M";

    [JsonProperty("hotkey_open_launcher")]
    public string HotkeyOpenLauncher { get; set; } = "Ctrl+Alt+G";

    [JsonProperty("launch_at_startup")]
    public bool LaunchAtStartup { get; set; } = false;

    [JsonProperty("minimize_to_tray")]
    public bool MinimizeToTray { get; set; } = true;

    [JsonProperty("ocr_enabled")]
    public bool OcrEnabled { get; set; } = true;

    [JsonProperty("voice_commands_enabled")]
    public bool VoiceCommandsEnabled { get; set; } = false;

    [JsonProperty("braille_enabled")]
    public bool BrailleEnabled { get; set; } = false;

    [JsonProperty("log_level")]
    public string LogLevel { get; set; } = "INFO";

    [JsonProperty("profile")]
    public string Profile { get; set; } = "standard";

    [JsonProperty("focus_highlight")]
    public bool FocusHighlight { get; set; } = true;

    [JsonProperty("piper_model_path")]
    public string PiperModelPath { get; set; } = @"resources\piper\voices";

    [JsonProperty("installed_plugins")]
    public List<string> InstalledPlugins { get; set; } = new();
}
