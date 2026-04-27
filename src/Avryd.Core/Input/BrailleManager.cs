using Avryd.Core.Speech;

namespace Avryd.Core.Input;

public class BrailleKey
{
    public bool Dot1 { get; set; }
    public bool Dot2 { get; set; }
    public bool Dot3 { get; set; }
    public bool Dot4 { get; set; }
    public bool Dot5 { get; set; }
    public bool Dot6 { get; set; }
    public bool Dot7 { get; set; }
    public bool Dot8 { get; set; }
    public bool Space { get; set; }

    public int Pattern =>
        (Dot1 ? 1 : 0) | (Dot2 ? 2 : 0) | (Dot3 ? 4 : 0) | (Dot4 ? 8 : 0) |
        (Dot5 ? 16 : 0) | (Dot6 ? 32 : 0) | (Dot7 ? 64 : 0) | (Dot8 ? 128 : 0);
}

public class BrailleManager : IDisposable
{
    private readonly SpeechManager _speech;
    private bool _disposed;
    private bool _enabled;
    private bool _eightDot;

    private static readonly Dictionary<int, char> Braille6DotToChar = new()
    {
        { 0b000001, 'a' }, { 0b000011, 'b' }, { 0b001001, 'c' }, { 0b011001, 'd' },
        { 0b010001, 'e' }, { 0b001011, 'f' }, { 0b011011, 'g' }, { 0b010011, 'h' },
        { 0b001010, 'i' }, { 0b011010, 'j' }, { 0b000101, 'k' }, { 0b000111, 'l' },
        { 0b001101, 'm' }, { 0b011101, 'n' }, { 0b010101, 'o' }, { 0b001111, 'p' },
        { 0b011111, 'q' }, { 0b010111, 'r' }, { 0b001110, 's' }, { 0b011110, 't' },
        { 0b100101, 'u' }, { 0b100111, 'v' }, { 0b111010, 'w' }, { 0b101101, 'x' },
        { 0b111101, 'y' }, { 0b110101, 'z' },
        { 0b000000, ' ' }
    };

    public event EventHandler<char>? CharacterEntered;
    public event EventHandler<string>? CommandEntered;

    public bool IsEnabled => _enabled;

    public BrailleManager(SpeechManager speech)
    {
        _speech = speech;
    }

    public void Enable(bool eightDot = false)
    {
        _enabled = true;
        _eightDot = eightDot;
        _speech.Speak($"Braille input enabled, {(eightDot ? "8" : "6")}-dot mode");
    }

    public void Disable()
    {
        _enabled = false;
        _speech.Speak("Braille input disabled");
    }

    public void ProcessKey(BrailleKey key)
    {
        if (!_enabled) return;

        var pattern = key.Pattern & 0x3F; // 6-dot
        if (_eightDot) pattern = key.Pattern; // 8-dot

        if (key.Space && key.Dot1 && key.Dot2 && key.Dot3)
        {
            CommandEntered?.Invoke(this, "stop");
            return;
        }

        if (Braille6DotToChar.TryGetValue(pattern, out var c))
        {
            CharacterEntered?.Invoke(this, c);
            _speech.Speak(c == ' ' ? "space" : c.ToString(), SpeechPriority.High);
        }
    }

    public string PatternToDescription(int pattern)
    {
        var dots = new List<int>();
        for (var i = 0; i < 8; i++)
            if ((pattern & (1 << i)) != 0) dots.Add(i + 1);
        return dots.Any() ? "dots " + string.Join(" ", dots) : "space";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}
