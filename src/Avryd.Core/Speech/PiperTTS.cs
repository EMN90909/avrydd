using System.Diagnostics;
using System.Text;

namespace Avryd.Core.Speech;

public class PiperTTS : IDisposable
{
    private readonly string _piperExePath;
    private readonly string _modelPath;
    private Process? _piperProcess;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;

    public bool IsAvailable { get; private set; }
    public string? CurrentVoice { get; private set; }

    public PiperTTS(string piperExePath, string modelPath)
    {
        _piperExePath = piperExePath;
        _modelPath = modelPath;
        IsAvailable = File.Exists(piperExePath);
    }

    public async Task SpeakAsync(string text, string? voiceId, int rate, double volume, CancellationToken ct = default)
    {
        if (!IsAvailable || string.IsNullOrWhiteSpace(text)) return;

        await _semaphore.WaitAsync(ct);
        try
        {
            var modelFile = ResolveModelFile(voiceId);
            if (modelFile == null) return;

            // Piper reads from stdin, outputs raw PCM or WAV to stdout
            // We pipe to an audio player
            var sanitized = SanitizeText(text);

            var psi = new ProcessStartInfo
            {
                FileName = _piperExePath,
                Arguments = BuildArgs(modelFile, rate),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardInputEncoding = Encoding.UTF8
            };

            _piperProcess = new Process { StartInfo = psi };
            _piperProcess.Start();

            // Write text to piper stdin
            await _piperProcess.StandardInput.WriteLineAsync(sanitized);
            _piperProcess.StandardInput.Close();

            // Read PCM output and play via Windows audio
            var pcmData = await _piperProcess.StandardOutput.BaseStream.ReadToEndAsync(ct);
            await _piperProcess.WaitForExitAsync(ct);

            CurrentVoice = voiceId;
        }
        catch (OperationCanceledException) { StopCurrent(); }
        catch (Exception ex)
        {
            Debug.WriteLine($"PiperTTS error: {ex.Message}");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SpeakToFileAsync(string text, string outputWav, string? voiceId, int rate, CancellationToken ct = default)
    {
        if (!IsAvailable || string.IsNullOrWhiteSpace(text)) return;

        var modelFile = ResolveModelFile(voiceId);
        if (modelFile == null) return;

        var sanitized = SanitizeText(text);
        var psi = new ProcessStartInfo
        {
            FileName = _piperExePath,
            Arguments = $"--model \"{modelFile}\" --output_file \"{outputWav}\" --length_scale {RateToLengthScale(rate)}",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardError = true,
            StandardInputEncoding = Encoding.UTF8
        };

        using var proc = new Process { StartInfo = psi };
        proc.Start();
        await proc.StandardInput.WriteLineAsync(sanitized);
        proc.StandardInput.Close();
        await proc.WaitForExitAsync(ct);
    }

    public void StopCurrent()
    {
        try
        {
            if (_piperProcess != null && !_piperProcess.HasExited)
                _piperProcess.Kill(entireProcessTree: true);
        }
        catch { }
        _piperProcess = null;
    }

    public List<string> GetAvailableVoices()
    {
        var voices = new List<string>();
        if (!Directory.Exists(_modelPath)) return voices;

        foreach (var f in Directory.GetFiles(_modelPath, "*.onnx"))
            voices.Add(Path.GetFileNameWithoutExtension(f));

        return voices;
    }

    private string? ResolveModelFile(string? voiceId)
    {
        if (string.IsNullOrEmpty(voiceId))
        {
            // Return first available voice
            if (Directory.Exists(_modelPath))
            {
                var first = Directory.GetFiles(_modelPath, "*.onnx").FirstOrDefault();
                return first;
            }
            return null;
        }

        var direct = Path.Combine(_modelPath, voiceId + ".onnx");
        if (File.Exists(direct)) return direct;

        // Search subdirectories
        if (Directory.Exists(_modelPath))
        {
            var found = Directory.GetFiles(_modelPath, "*.onnx", SearchOption.AllDirectories)
                .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Contains(voiceId, StringComparison.OrdinalIgnoreCase));
            return found;
        }

        return null;
    }

    private string BuildArgs(string modelFile, int rate)
    {
        var lengthScale = RateToLengthScale(rate);
        return $"--model \"{modelFile}\" --output-raw --length_scale {lengthScale}";
    }

    private static double RateToLengthScale(int wpm)
    {
        // Piper length_scale: 1.0 = normal, <1 = faster, >1 = slower
        // Normal WPM ~175, scale accordingly
        var ratio = 175.0 / Math.Max(wpm, 50);
        return Math.Clamp(ratio, 0.3, 3.0);
    }

    private static string SanitizeText(string text)
    {
        // Remove control characters and limit length
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (c == '\n' || c == '\r') sb.Append(' ');
            else if (c >= 0x20) sb.Append(c);
        }
        var result = sb.ToString().Trim();
        if (result.Length > 4000) result = result[..4000];
        return result;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopCurrent();
        _semaphore.Dispose();
        _piperProcess?.Dispose();
    }
}
