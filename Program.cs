using Modern.WindowKit;
using Modern.WindowKit.Controls.Platform.Surfaces;
using Modern.WindowKit.Input.Raw;
using Modern.WindowKit.Platform;
using Modern.WindowKit.Skia;
using Modern.WindowKit.Threading;
using SkiaSharp;

namespace AStartTest;

public static class Program
{
    private static IWindowImpl _window = null!;
    private static SKSurface? _surface;
    private static SKCanvas _canvas = null!;

    // ReSharper disable once InconsistentNaming
    private static readonly SKPaint _paint = new();

    
    private static void Main()
    {
        _window = AvaloniaGlobals.GetRequiredService<IWindowingPlatform>().CreateWindow();
        _window.Resize(new Size(1024, 768));
        _window.SetTitle("Modern.WindowKit Demo");
        _window.SetIcon(SKBitmap.Decode("icon.png"));

        var mainLoopCancellationTokenSource = new CancellationTokenSource();
        _window.Closed = () => mainLoopCancellationTokenSource.Cancel();

        _window.Resized = (_, _) => { _surface?.Dispose(); _surface = null; };

        _window.PositionChanged = _ => Invalidate();

        _window.Paint = DoPaint;

        _window.Input = HandleInput;

        _window.Show(true, false);

        Dispatcher.UIThread.MainLoop(mainLoopCancellationTokenSource.Token);
    }

    private static SKSurface GetCanvas()
    {
        if (_surface is not null)
            return _surface;

        var screen = _window.ClientSize * _window.RenderScaling;
        var info = new SKImageInfo((int)screen.Width, (int)screen.Height);

        _surface = SKSurface.Create(info);
        _surface.Canvas.Clear(SKColors.White);
        
        return _surface;
    }

    private static void DoPaint(Rect bounds)
    {
        var skiaFramebuffer = _window.Surfaces.OfType<IFramebufferPlatformSurface>().First();

        using var framebuffer = skiaFramebuffer.Lock();

        var framebufferImageInfo = new SKImageInfo(framebuffer.Size.Width, framebuffer.Size.Height,
            framebuffer.Format.ToSkColorType(), framebuffer.Format == PixelFormat.Rgb565 ? SKAlphaType.Opaque : SKAlphaType.Premul);

        using var surface = SKSurface.Create(framebufferImageInfo, framebuffer.Address, framebuffer.RowBytes);

        surface.Canvas.DrawSurface(GetCanvas(), SKPoint.Empty);
        _canvas = surface.Canvas;
        
        Paint();
    }

    private static void Paint()
    {
        const int cellSize = 100;

        var horizontalCellCount = (int)_window.ClientSize.Width / cellSize;
        var verticalCellCount = (int)_window.ClientSize.Height / cellSize;

        _paint.Color = SKColors.Green;
        _paint.StrokeWidth = 5;
        
        for (var i = 0; i <= horizontalCellCount; i++)
        {
            _canvas.DrawLineScale(i * cellSize, 0, i * cellSize, (float)_window.ClientSize.Height, _paint);
        }

        for (var i = 0; i <= verticalCellCount; i++)
        {
            _canvas.DrawLineScale(0, i * cellSize, (float)_window.ClientSize.Width, i * cellSize, _paint);
        }
    }

    private static void DrawLineScale(this SKCanvas canvas, float x, float y, float w, float h, SKPaint paint)
    {
        _canvas.DrawLine(Scale(x), Scale(y), Scale(w), Scale(h), paint);
    }

    private static void HandleInput(RawInputEventArgs obj)
    {
        if (obj is RawPointerEventArgs pointer)
            HandleMouseInput(pointer);
    }

    private static void HandleMouseInput(RawPointerEventArgs e)
    {
        var x = Scale((int)e.Position.X);
        var y = Scale((int)e.Position.Y);

        Invalidate();
    }
    
    private static void Invalidate() => _window.Invalidate(new Rect(Point.Empty, _window.ClientSize));
    
    private static float Scale(float value) => (float)(value * _window.RenderScaling);
}