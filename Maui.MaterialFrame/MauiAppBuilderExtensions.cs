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
#elif IOS || MACCATALYST
            handlers.AddHandler<MaterialFrame, iOS.iOSMaterialFrameHandler>();
#elif WINDOWS
            handlers.AddHandler<MaterialFrame, WinUI.WinUIMaterialFrameHandler>();
#endif
        });

        return builder;
    }
}
