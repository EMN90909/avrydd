using Avryd.Core.Models;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace Avryd.Core.Settings;

public class SettingsManager
{
    private static readonly string AppDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Avryd");
    private static readonly string SettingsFile = Path.Combine(AppDataPath, "settings.json");
    private static readonly string ActivationFile = Path.Combine(AppDataPath, "activation.dat");
    private static readonly string ProfileFile = Path.Combine(AppDataPath, "profile.json");

    private AppSettings _settings = new();
    private ActivationInfo? _activation;
    private UserProfile? _profile;

    public AppSettings Settings => _settings;
    public ActivationInfo? Activation => _activation;
    public UserProfile? Profile => _profile;

    public bool IsFirstRun => !File.Exists(SettingsFile);
    public bool IsActivated => _activation?.SessionToken != null &&
                               File.Exists(ActivationFile);

    public void Load()
    {
        Directory.CreateDirectory(AppDataPath);

        if (File.Exists(SettingsFile))
        {
            try
            {
                var json = File.ReadAllText(SettingsFile);
                _settings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            }
            catch { _settings = new AppSettings(); }
        }

        if (File.Exists(ActivationFile))
        {
            try
            {
                var encrypted = File.ReadAllText(ActivationFile);
                var json = DecryptString(encrypted);
                _activation = JsonConvert.DeserializeObject<ActivationInfo>(json);
            }
            catch { _activation = null; }
        }

        if (File.Exists(ProfileFile))
        {
            try
            {
                var json = File.ReadAllText(ProfileFile);
                _profile = JsonConvert.DeserializeObject<UserProfile>(json);
            }
            catch { _profile = null; }
        }
    }

    public void SaveSettings()
    {
        Directory.CreateDirectory(AppDataPath);
        var json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
        File.WriteAllText(SettingsFile, json);
    }

    public void SaveActivation(ActivationInfo activation)
    {
        Directory.CreateDirectory(AppDataPath);
        _activation = activation;
        var json = JsonConvert.SerializeObject(activation);
        var encrypted = EncryptString(json);
        File.WriteAllText(ActivationFile, encrypted);
    }

    public void SaveProfile(UserProfile profile)
    {
        Directory.CreateDirectory(AppDataPath);
        _profile = profile;
        var json = JsonConvert.SerializeObject(profile, Formatting.Indented);
        File.WriteAllText(ProfileFile, json);
    }

    public void ClearActivation()
    {
        _activation = null;
        if (File.Exists(ActivationFile))
            File.Delete(ActivationFile);
    }

    public void ApplyProfile(string profileName)
    {
        switch (profileName.ToLower())
        {
            case "fast":
                _settings.Rate = 250;
                _settings.Verbosity = "minimal";
                _settings.PunctuationLevel = "none";
                break;
            case "new_user":
                _settings.Rate = 150;
                _settings.Verbosity = "verbose";
                _settings.PunctuationLevel = "all";
                _settings.SpeakTooltips = true;
                break;
            case "low_vision":
                _settings.Rate = 175;
                _settings.FocusHighlight = true;
                _settings.SoundEffects = true;
                break;
            case "quiet":
                _settings.Volume = 0.5;
                _settings.SpeakNotifications = false;
                _settings.SoundEffects = false;
                _settings.HapticFeedback = false;
                break;
            default:
                break;
        }
        _settings.Profile = profileName;
        SaveSettings();
    }

    public void ResetToDefaults()
    {
        _settings = new AppSettings();
        SaveSettings();
    }

    public string ExportSettings()
    {
        return JsonConvert.SerializeObject(_settings, Formatting.Indented);
    }

    public void ImportSettings(string json)
    {
        var imported = JsonConvert.DeserializeObject<AppSettings>(json);
        if (imported != null)
        {
            _settings = imported;
            SaveSettings();
        }
    }

    private static string EncryptString(string plainText)
    {
        var key = GetMachineKey();
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        var result = new byte[aes.IV.Length + encrypted.Length];
        aes.IV.CopyTo(result, 0);
        encrypted.CopyTo(result, aes.IV.Length);
        return Convert.ToBase64String(result);
    }

    private static string DecryptString(string cipherText)
    {
        var key = GetMachineKey();
        var fullBytes = Convert.FromBase64String(cipherText);
        using var aes = Aes.Create();
        aes.Key = key;
        var iv = fullBytes[..16];
        var data = fullBytes[16..];
        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        var decrypted = decryptor.TransformFinalBlock(data, 0, data.Length);
        return Encoding.UTF8.GetString(decrypted);
    }

    private static byte[] GetMachineKey()
    {
        var raw = Environment.MachineName + Environment.UserName + "Avryd_SecureKey_2024";
        using var sha = SHA256.Create();
        return sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
    }
}
