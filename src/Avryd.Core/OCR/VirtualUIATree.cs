using Avryd.Core.UIA;
using System.Windows.Automation;

namespace Avryd.Core.OCR;

public class VirtualNode
{
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = "Text";
    public string Value { get; set; } = string.Empty;
    public System.Windows.Rect Bounds { get; set; }
    public List<VirtualNode> Children { get; set; } = new();
    public bool IsInteractive { get; set; }
}

public class VirtualUIATree
{
    private readonly List<UIAElement> _virtualElements = new();
    private int _currentIndex = -1;

    public IReadOnlyList<UIAElement> Elements => _virtualElements;
    public int Count => _virtualElements.Count;

    public void BuildFromOcr(OcrResult ocr, System.Windows.Rect windowBounds)
    {
        _virtualElements.Clear();

        if (!ocr.Success) return;

        foreach (var word in ocr.Words.Where(w => w.Confidence > 0.5f && !string.IsNullOrWhiteSpace(w.Text)))
        {
            _virtualElements.Add(new UIAElement
            {
                Name = word.Text,
                Role = "Text",
                BoundingRect = new System.Windows.Rect(
                    windowBounds.X + word.Bounds.X,
                    windowBounds.Y + word.Bounds.Y,
                    word.Bounds.Width,
                    word.Bounds.Height)
            });
        }

        MergeIntoLines();
    }

    private void MergeIntoLines()
    {
        if (!_virtualElements.Any()) return;

        var lines = new List<UIAElement>();
        var sorted = _virtualElements.OrderBy(e => e.BoundingRect.Top).ThenBy(e => e.BoundingRect.Left).ToList();

        var currentLine = new List<UIAElement> { sorted[0] };
        var currentY = sorted[0].BoundingRect.Top;

        for (var i = 1; i < sorted.Count; i++)
        {
            var el = sorted[i];
            if (Math.Abs(el.BoundingRect.Top - currentY) < 10)
            {
                currentLine.Add(el);
            }
            else
            {
                lines.Add(MergeLine(currentLine));
                currentLine = new List<UIAElement> { el };
                currentY = el.BoundingRect.Top;
            }
        }

        if (currentLine.Any()) lines.Add(MergeLine(currentLine));

        _virtualElements.Clear();
        _virtualElements.AddRange(lines);
    }

    private static UIAElement MergeLine(List<UIAElement> words)
    {
        var text = string.Join(" ", words.Select(w => w.Name));
        var left = words.Min(w => w.BoundingRect.Left);
        var top = words.Min(w => w.BoundingRect.Top);
        var right = words.Max(w => w.BoundingRect.Right);
        var bottom = words.Max(w => w.BoundingRect.Bottom);

        return new UIAElement
        {
            Name = text,
            Role = "Text",
            BoundingRect = new System.Windows.Rect(left, top, right - left, bottom - top)
        };
    }

    public UIAElement? GetNext()
    {
        if (_currentIndex < _virtualElements.Count - 1)
        {
            _currentIndex++;
            return _virtualElements[_currentIndex];
        }
        return null;
    }

    public UIAElement? GetPrevious()
    {
        if (_currentIndex > 0)
        {
            _currentIndex--;
            return _virtualElements[_currentIndex];
        }
        return null;
    }

    public void Reset() => _currentIndex = -1;
}
