using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using Sharpnado.MaterialFrame;

namespace Sharpnado.Acrylic.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseSharpnadoMaterialFrame(true, true)
            .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("OpenSans-SemiboldItalic.ttf", "OpenSansSemiboldItalic");
                    fonts.AddFont("OpenSans-Bold.ttf", "OpenSansBold");
                    fonts.AddFont("OpenSans-BoldItalic.ttf", "OpenSansBoldItalic");
                    fonts.AddFont("OpenSans-ExtraBold.ttf", "OpenSansExtraBold");
                    fonts.AddFont("OpenSans-ExtraBoldItalic.ttf", "OpenSansExtraBoldItalic");
                    fonts.AddFont("OpenSans-Italic.ttf", "OpenSansItalic");
                    fonts.AddFont("OpenSans-Light.ttf", "OpenSansLight");
                    fonts.AddFont("OpenSans-LightItalic.ttf", "OpenSansLightItalic");
                    fonts.AddFont("OpenSans-Bold.ttf", "OpenSansBold");
                    fonts.AddFont("Font-Awesome-5-Free-Solid-900.otf", "FontAwesome");
                    fonts.AddFont("Font-Awesome-5-Free-Regular-400.otf", "FontAwesomeRegular");
                }
            )
            .ConfigureLifecycleEvents(events =>
            {
#if ANDROID
                events.AddAndroid(android => android.OnCreate((activity, _) =>
                {
                    activity.Window.SetFlags(
                        Android.Views.WindowManagerFlags.LayoutNoLimits,
                        Android.Views.WindowManagerFlags.LayoutNoLimits);
                    activity.Window.ClearFlags(Android.Views.WindowManagerFlags.TranslucentStatus);
                    activity.Window.SetStatusBarColor(Android.Graphics.Color.Transparent);
                }));
#endif
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
