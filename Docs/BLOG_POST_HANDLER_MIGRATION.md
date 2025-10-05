# Migrating MaterialFrame to MAUI Handlers: A StackBlur Story

HI THERE! Long time no see eeeeh? Well I have been kind of busy, some deep diving into MaterialFrame internals, and I must say: what started as a simple handler migration turned into a complete Android blur rewrite.

Remember when MaterialFrame had that sweet blur effect using RenderScript? Yeah that was nice... until Android 15 came along with its 16KB page size and decided to completely break it. Like REALLY GOOGLE???

But rejoice! This forced rewrite turned into something way better. Let me tell you about it.

---

## The Handler Migration

So first things first: renderers are dead, long live handlers! I know, we all procrastinated on this migration because the compatibility layer was working just fine thank you very much. But since MAUI 9.0, it's very nice! Time to bite the bullet.

Remember the old renderer pattern? It looked something like this:

```csharp
protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
{
    base.OnElementPropertyChanged(sender, e);
    
    if (e.PropertyName == nameof(MaterialFrame.CornerRadius))
        UpdateCornerRadius();
    else if (e.PropertyName == nameof(MaterialFrame.Elevation))
        UpdateElevation();
    // ... 50 more else ifs
}
```

Yeah, not fun. Now with handlers it's all nice and declarative:

```csharp
public static PropertyMapper<MaterialFrame, AndroidMaterialFrameHandler> MaterialFrameMapper = new(Mapper)
{
    [nameof(MaterialFrame.CornerRadius)] = (handler, view) => handler.UpdateCornerRadius(),
    [nameof(MaterialFrame.Elevation)] = (handler, view) => handler.UpdateElevation(),
    [nameof(MaterialFrame.MaterialTheme)] = (handler, view) => handler.UpdateMaterialTheme(),
};
```

Much better! You can see everything at a glance, it's type-safe, and MAUI can optimize this under the hood.

All platforms migrated:
- ✅ Android: ContentViewHandler
- ✅ iOS: ContentViewHandler  
- ✅ MacCatalyst: ContentViewHandler (I just copied the iOS code, don't @ me)
- ✅ Windows: ViewHandler<MaterialFrame, Grid>

Windows was special because we render to a Grid directly with all the composition shadow stuff. But it works \o/

---

## The Android Drama: RenderScript Is Dead

Here's where it gets interesting. The original Android blur was using RenderScript. You know, that thing Google told us to use and then deprecated? Yeah that one.

It was working great! Fast, smooth, beautiful blurs... and then Pixel 8 users started reporting crashes. Turns out on devices with 16KB page size (Android 15+), RenderScript just... doesn't work anymore. At all. Complete breakage.

So I had a choice:
1. Wait for a fix that's never coming (deprecated remember?)
2. Find another way

Guess which one I picked? 🙃

---

## StackBlur to the Rescue

After some research, I found **StackBlur** by Mario Klingemann. It's an elegant CPU-based blur algorithm that's surprisingly fast.

The beauty of StackBlur? It's **pure C#**. No native libs, no GPU drivers, no Android version drama. Just good old-fashioned sliding window arithmetic.

```csharp
private void StackBlurHorizontal(int[] pixels, int w, int h, int radius)
{
    int div = radius + radius + 1;
    
    for (int y = 0; y < h; y++)
    {
        int sumR = 0, sumG = 0, sumB = 0;
        
        // Build initial stack
        for (int x = -radius; x <= radius; x++)
        {
            int pixel = pixels[y * w + Math.Clamp(x, 0, w - 1)];
            sumR += (pixel >> 16) & 0xFF;
            sumG += (pixel >> 8) & 0xFF;
            sumB += pixel & 0xFF;
        }
        
        // Slide the window
        for (int x = 0; x < w; x++)
        {
            pixels[y * w + x] = (sumR / div << 16) | (sumG / div << 8) | (sumB / div);
            // ... update sums
        }
    }
}
```

Performance? About **15-25ms for a 500x500px image**. Not bad for pure C#!

But that's not all...

---

## Making It Buttery Smooth: Double Buffering

15-25ms is not bad, but that's still way too much to run on the UI thread. The app would stutter like crazy during scrolling.

The solution? **Move it off-thread!** But naive async won't cut it - you need proper buffering or you get tearing and race conditions.

So double buffering:

```csharp
private Bitmap? _frontBuffer;  // What's displayed right now
private Bitmap? _backBuffer;   // What we're working on
private volatile bool _isProcessing;

public void ProcessBlurAsync(Bitmap source)
{
    if (_isProcessing) return; // One job at a time!
    
    _isProcessing = true;
    
    Task.Run(() =>
    {
        // Process in background
        ApplyStackBlur(_backBuffer, source, radius: 25);
        
        // Swap on UI thread
        MainThread.BeginInvokeOnMainThread(() =>
        {
            (_frontBuffer, _backBuffer) = (_backBuffer, _frontBuffer);
            InvalidateBlurLayer();
            _isProcessing = false;
        });
    });
}
```

**Results:** UI thread blocking went from 22ms to **3ms**. Frame drops? Gone. Smooth 60 FPS scrolling? Check! ✨

---

## The Lazy Optimization: Change Detection

I was watching the profiler and noticed something: when the user stops scrolling, we're still blurring like crazy. Every frame. For no reason.

The background didn't change, so why reprocess it?

So the world's simplest change detection:

```csharp
private int ComputeContentHash(Bitmap bitmap)
{
    const int samplePoints = 16; // Sample a 4x4 grid
    int hash = 17;
    
    int stepX = bitmap.Width / 4;
    int stepY = bitmap.Height / 4;
    
    for (int y = 0; y < 4; y++)
    {
        for (int x = 0; x < 4; x++)
        {
            int pixel = bitmap.GetPixel(x * stepX, y * stepY);
            hash = hash * 31 + pixel;
        }
    }
    
    return hash;
}
```

We sample 16 pixels in a grid pattern, hash them, and compare. If the hash matches the previous frame? Skip the blur entirely!

**Results:** When content is static, CPU usage drops to **0%**. Battery life? Way better. Thermal throttling? Reduced.

---

## iOS and Mac: The Easy Ones

iOS and MacCatalyst were pretty straightforward. Migrated to ContentViewHandler, kept using UIVisualEffectView:

```csharp
private void EnableBlur()
{
    var blurEffect = UIBlurEffect.FromStyle(GetBlurStyle());
    _blurView = new UIVisualEffectView(blurEffect);
    PlatformView.Layer.InsertSublayer(_blurView.Layer, 0);
}
```

Native blur is hardware-accelerated, respects system settings, and just works™. Apple got this right.

MacCatalyst? I copy-pasted the iOS handler into the MacCatalyst folder. Don't @ me. 😎

---

## Windows: AcrylicBrush FTW

Windows migration was smooth. Went from ViewRenderer to ViewHandler<MaterialFrame, Grid>:

```csharp
private void UpdateBlur()
{
    var acrylicBrush = new AcrylicBrush
    {
        TintColor = GetTintColor(),
        FallbackColor = GetFallbackColor(),
    };
    
    _acrylicGrid.Background = acrylicBrush;
}
```

Windows acrylic just... works. It's performant, it's pretty, and it has fallback for older systems. Thanks Microsoft!

---

## The Numbers (Because We Love Numbers)

So what did we actually achieve here?

| Metric | Before | After | Change |
|--------|--------|-------|--------|
| Android 15 compatibility | 💥 Broken | ✅ Works | ∞% better |
| UI thread time | 22ms | 3ms | **86% faster** |
| Static content CPU | 100% | 0% | **100% reduction** |
| Frame rate | 30-45 FPS | 60 FPS | Smooth AF |
| Blur quality | Great | Great | Still great |

Not bad for a "forced rewrite" eh?

---

## What I Learned

**Sometimes Breaking Changes Are Good**

RenderScript breaking forced me to find a better solution. StackBlur is simpler, more portable, and performs better in practice.

**Async + Buffering = Magic**

Never block the UI thread. With double buffering, background work becomes seamless.

**Don't Compute What You Don't Need**

Change detection is such a simple idea but the impact is huge. Always measure first though!

**The Handler Pattern Rocks**

I was skeptical at first, but the new handler pattern is really nice. More maintainable, more performant, cleaner code.

---

## Installation

Sharpnado.MaterialFrame 2.0 is out with all this goodness:

```bash
dotnet add package Sharpnado.MaterialFrame.Maui --version 2.0.0
```

```csharp
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    builder
        .UseMauiApp<App>()
        .UseSharpnadoMaterialFrame(loggerEnable: false);
}
```

```xaml
<sh:MaterialFrame MaterialTheme="AcrylicBlur"
                  MaterialBlurStyle="Light"
                  CornerRadius="10"
                  Elevation="8">
    <Label Text="Smooth like butter 🧈" />
</sh:MaterialFrame>
```

BOOM You just achieved smooth 60 FPS blur on all platforms.

---

## What's Next?

I'm not done yet! Future optimizations:

- **ARM NEON SIMD** - Hardware-accelerated blur on ARM processors
- **Adaptive downsampling** - Reduce resolution when scrolling fast
- **Frame rate throttling** - Match blur updates to actual scroll speed
- **Buffer pooling** - Reduce GC pressure

But for now, this is solid. Android 15 works, performance is great, code is cleaner.

---

## Wrapping Up

Migrating to handlers turned into a complete Android blur rewrite, which turned into a performance optimization journey. Sometimes the universe forces you to improve your code.

If you're still using renderers in your MAUI libs, now's the time. MAUI 9 is solid, the handler pattern is mature, your future self will thank you.

Questions or ideas? [github.com/roubachof/Sharpnado.MaterialFrame](https://github.com/roubachof/Sharpnado.MaterialFrame)

Happy blurring! ✨

---

*Jean-Marie Alfonsi*

---

## Further Reading

- Full blur optimization docs: [BLUR_OPTIMIZATIONS.md](./BLUR_OPTIMIZATIONS.md)
- StackBlur algorithm deep-dive: [BLUR_STACKBLUR.md](./BLUR_STACKBLUR.md)
- Async blur implementation: [BLUR_ASYNC.md](./BLUR_ASYNC.md)
- [Mario Klingemann's original StackBlur](http://incubator.quasimondo.com/processing/fast_blur_deluxe.php)
