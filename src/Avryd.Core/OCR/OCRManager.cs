using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using Tesseract;

namespace Avryd.Core.OCR;

public class OcrResult
{
    public string Text { get; set; } = string.Empty;
    public List<OcrWord> Words { get; set; } = new();
    public bool Success { get; set; }
}

public class OcrWord
{
    public string Text { get; set; } = string.Empty;
    public Rectangle Bounds { get; set; }
    public float Confidence { get; set; }
}

public class OCRManager : IDisposable
{
    [DllImport("user32.dll")] private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    private TesseractEngine? _engine;
    private bool _disposed;
    private bool _available;

    public bool IsAvailable => _available;

    public OCRManager(string tessDataPath)
    {
        try
        {
            if (Directory.Exists(tessDataPath))
            {
                _engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default);
                _available = true;
            }
        }
        catch { _available = false; }
    }

    public async Task<OcrResult> CaptureAndRecognizeWindowAsync(IntPtr hwnd)
    {
        return await Task.Run(() => CaptureAndRecognize(hwnd));
    }

    public async Task<OcrResult> CaptureAndRecognizeBoundsAsync(System.Windows.Rect bounds)
    {
        return await Task.Run(() =>
        {
            var rect = new Rectangle(
                (int)bounds.X, (int)bounds.Y,
                (int)bounds.Width, (int)bounds.Height);
            return RecognizeRegion(rect);
        });
    }

    private OcrResult CaptureAndRecognize(IntPtr hwnd)
    {
        var result = new OcrResult();
        try
        {
            if (!GetWindowRect(hwnd, out var rect)) return result;
            var width = rect.Right - rect.Left;
            var height = rect.Bottom - rect.Top;
            if (width <= 0 || height <= 0) return result;

            using var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            var hdc = g.GetHdc();
            PrintWindow(hwnd, hdc, 2);
            g.ReleaseHdc(hdc);

            return RecognizeBitmap(bmp);
        }
        catch { return result; }
    }

    private OcrResult RecognizeRegion(Rectangle region)
    {
        try
        {
            using var bmp = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            g.CopyFromScreen(region.Location, Point.Empty, region.Size);
            return RecognizeBitmap(bmp);
        }
        catch { return new OcrResult(); }
    }

    private OcrResult RecognizeBitmap(Bitmap bmp)
    {
        var result = new OcrResult();
        if (!_available || _engine == null) return result;

        try
        {
            using var ms = new System.IO.MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            ms.Position = 0;

            using var pix = Pix.LoadFromMemory(ms.ToArray());
            using var page = _engine.Process(pix);
            result.Text = page.GetText()?.Trim() ?? string.Empty;
            result.Success = !string.IsNullOrEmpty(result.Text);

            using var iter = page.GetIterator();
            iter.Begin();
            do
            {
                if (iter.TryGetBoundingBox(PageIteratorLevel.Word, out var box))
                {
                    result.Words.Add(new OcrWord
                    {
                        Text = iter.GetText(PageIteratorLevel.Word)?.Trim() ?? string.Empty,
                        Bounds = new Rectangle(box.X1, box.Y1, box.Width, box.Height),
                        Confidence = iter.GetConfidence(PageIteratorLevel.Word) / 100f
                    });
                }
            } while (iter.Next(PageIteratorLevel.Word));
        }
        catch { }
        return result;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _engine?.Dispose();
    }
}
