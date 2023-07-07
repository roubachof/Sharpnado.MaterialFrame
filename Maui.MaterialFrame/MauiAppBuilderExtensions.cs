using Microsoft.Maui.Controls.Compatibility.Hosting;

namespace Sharpnado.MaterialFrame;

public static class MauiAppBuilderExtensions
{
    public static MauiAppBuilder UseSharpnadoMaterialFrame(
        this MauiAppBuilder builder,
        bool loggerEnable,
        bool debugLogEnable = false)
    {
        Initializer.Initialize(loggerEnable, debugLogEnable);

        builder.UseMauiCompatibility();
        builder.ConfigureMauiHandlers(handlers =>
        {
#if ANDROID
            handlers.AddHandler<MaterialFrame, Sharpnado.MaterialFrame.Droid.AndroidMaterialFrameRenderer>();
#elif IOS
            handlers.AddHandler<MaterialFrame, Sharpnado.MaterialFrame.iOS.iOSMaterialFrameRenderer>();
#elif MACCATALYST
            handlers.AddHandler<MaterialFrame, Sharpnado.MaterialFrame.MacCatalyst.MacCatalystMaterialFrameRenderer>();
#elif WINDOWS
            // TODO
            // handlers.AddHandler<MaterialFrame, Sharpnado.MaterialFrame.MacCatalyst.MacCatalystMaterialFrameRenderer>();
#endif
        });

        return builder;
    }
}
