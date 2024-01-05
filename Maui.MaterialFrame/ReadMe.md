# Sharpnado.MaterialFrame


| Supported platforms        |
|----------------------------|
| :heavy_check_mark: Android | 
| :heavy_check_mark: iOS     |
| :question: macOS   |
| :heavy_check_mark: WinUI     |

## Initialization

* In `MauiProgram.cs`:

```csharp
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    builder
        .UseMauiApp<App>()
        .UseSharpnadoMaterialFrame(loggerEnable: false)
        ...
}
```

## Mac Catalyst has not been tested yet

But it should be working :) ?

## Android Compatibility issues

Warning, because of `LayerDrawable` the `Acrylic` glow effect (the white glow on the top of the `MaterialFrame` is only available on API 23+ (since Marshmallow).

## iOS limitations

For some yet to be discovered reasons, `AcrylicBlur` value doesn't work in a dynamic context on iOS.

You can change the BlurStyle dynamically, but a dynamic change from a not blurry theme to the `AcrylicBlur` theme will result in a transparent frame.

## Presentation

The Xamarin.Forms `MaterialFrame` aims at delivering out of the box modern popular theming such as:
  * Light
  * Dark
  * Acrylic
  * AcrylicBlur

You can switch from one theme to another thanks to the `MaterialFrame` property.
