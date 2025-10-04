namespace Sharpnado.MaterialFrame;

public static class MauiAppBuilderExtensions
{
    public static MauiAppBuilder UseSharpnadoMaterialFrame(
        this MauiAppBuilder builder,
        bool loggerEnable,
        bool debugLogEnable = false)
    {
        Initializer.Initialize(loggerEnable, debugLogEnable);

        builder.ConfigureMauiHandlers(handlers =>
        {
#if ANDROID
            handlers.AddHandler<MaterialFrame, Droid.AndroidMaterialFrameHandler>();
#elif IOS
            handlers.AddHandler<MaterialFrame, iOS.iOSMaterialFrameHandler>();
#elif MACCATALYST
            handlers.AddHandler<MaterialFrame, MacCatalyst.MacCatalystMaterialFrameRenderer>();
#elif WINDOWS
            handlers.AddHandler<MaterialFrame, WinUI.WinUIMaterialFrameRenderer>();
#endif
        });

        return builder;
    }
}
