using Android.Graphics.Drawables;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Sharpnado.MaterialFrame.Droid;

public partial class AndroidMaterialFrameHandler() : ContentViewHandler(MaterialFrameMapper)
{
    public static PropertyMapper<MaterialFrame, AndroidMaterialFrameHandler> MaterialFrameMapper = new(Mapper)
    {
        [nameof(IView.Background)] = (handler, view) => handler.UpdateBackground(),
        [nameof(MaterialFrame.CornerRadius)] = (handler, view) => handler.UpdateCornerRadius(),
        [nameof(MaterialFrame.Elevation)] = (handler, view) => handler.UpdateElevation(),
        [nameof(MaterialFrame.LightThemeBackgroundColor)] = (handler, view) => handler.UpdateLightThemeBackgroundColor(),
        [nameof(MaterialFrame.AcrylicGlowColor)] = (handler, view) => handler.UpdateAcrylicGlowColor(),
        [nameof(MaterialFrame.AndroidBlurOverlayColor)] = (handler, view) => handler.UpdateAndroidBlurOverlayColor(),
        [nameof(MaterialFrame.AndroidBlurRadius)] = (handler, view) => handler.UpdateAndroidBlurRadius(),
        [nameof(MaterialFrame.AndroidBlurRootElement)] = (handler, view) => handler.UpdateAndroidBlurRootElement(),
        [nameof(MaterialFrame.MaterialTheme)] = (handler, view) => handler.UpdateMaterialTheme(),
        [nameof(MaterialFrame.MaterialBlurStyle)] = (handler, view) => handler.UpdateMaterialBlurStyle(),
    };

    private const string Tag = nameof(AndroidMaterialFrameHandler);

    private GradientDrawable? _mainDrawable;

    private GradientDrawable? _acrylicLayer;

    private MaterialFrame MaterialFrame => (MaterialFrame)VirtualView;

    protected override ContentViewGroup CreatePlatformView()
    {
        InternalLogger.Debug(Tag, () => "CreatePlatformView()");

        var contentView = base.CreatePlatformView();

        return contentView;
    }

    protected override void ConnectHandler(ContentViewGroup platformView)
    {
        base.ConnectHandler(platformView);

        InternalLogger.Debug(Tag, () => "ConnectHandler()");

        platformView.SetBackground(new ColorDrawable(Android.Graphics.Color.Transparent));

        // Handle blur root element setup when view is attached
        if (MaterialFrame.AndroidBlurRootElement != null && _blurRootView == null)
        {
            platformView.Post(() =>
            {
                if (MaterialFrame.AndroidBlurRootElement != null && _blurRootView == null)
                {
                    UpdateAndroidBlurRootElement();
                }
            });
        }

        platformView.Post(UpdateMaterialTheme);
    }

    protected override void DisconnectHandler(ContentViewGroup platformView)
    {
        InternalLogger.Debug(Tag, () => "DisconnectHandler()");

        MaterialFrame.Unsubscribe();
        Destroy();

        base.DisconnectHandler(platformView);
    }

    private void Destroy()
    {
        InternalLogger.Debug(Tag, "Destroy()");

        _mainDrawable?.Dispose();
        _mainDrawable = null;

        DestroyBlur();

        if (!_acrylicLayer.IsNullOrDisposed())
        {
            _acrylicLayer!.Dispose();
            _acrylicLayer = null;
        }
    }

    private void UpdateBackground()
    {
        InternalLogger.Debug(Tag, () => "UpdateBackground()");

        if (MaterialFrame.Background is not SolidColorBrush solidColorBrush)
        {
            throw new ArgumentException(
                "MaterialFrame only supports SolidColorBrush as Background",
                nameof(MaterialFrame.Background));
        }

        _mainDrawable = new GradientDrawable();
        _mainDrawable.SetColor(solidColorBrush.Color?.ToPlatform() ?? Colors.Transparent.ToPlatform());

        PlatformView.Background = _mainDrawable;
    }

    private void UpdateCornerRadius()
    {
        InternalLogger.Debug(Tag, () => "UpdateCornerRadius()");
        float radiusInPixels = Context.ToPixels(MaterialFrame.CornerRadius);

        _mainDrawable?.SetCornerRadius(radiusInPixels);
        _acrylicLayer?.SetCornerRadius(radiusInPixels);
        _realtimeBlurView?.SetCornerRadius(radiusInPixels);

        // Set corner radius on the ContentViewGroup for shadow clipping
        if (radiusInPixels > 0)
        {
            PlatformView.SetClipToOutline(true);
            PlatformView.OutlineProvider = new AndroidOutlineProvider(radiusInPixels);
        }
    }

    private void UpdateElevation()
    {
        InternalLogger.Debug(Tag, () => "UpdateElevation()");
        if (MaterialFrame.MaterialTheme == MaterialFrame.Theme.Dark || MaterialFrame.MaterialTheme == MaterialFrame.Theme.AcrylicBlur)
        {
            PlatformView.Elevation = 0;
            return;
        }

        bool isAcrylicTheme = MaterialFrame.MaterialTheme == MaterialFrame.Theme.Acrylic;

        // we need to reset the StateListAnimator to override the setting of Elevation on touch down and release.
        PlatformView.StateListAnimator = new Android.Animation.StateListAnimator();

        // set the elevation manually
        float elevation = isAcrylicTheme ? MaterialFrame.AcrylicElevation : MaterialFrame.Elevation;
        PlatformView.Elevation = Context.ToPixels(elevation);
    }

    private void UpdateLightThemeBackgroundColor()
    {
        if (_mainDrawable == null)
        {
            return;
        }

        InternalLogger.Debug(Tag, () => "UpdateLightThemeBackgroundColor()");
        if (MaterialFrame.MaterialTheme == MaterialFrame.Theme.Dark || MaterialFrame.MaterialTheme == MaterialFrame.Theme.AcrylicBlur)
        {
            return;
        }

        _mainDrawable.SetColor(MaterialFrame.LightThemeBackgroundColor.ToPlatform());
    }

    private void UpdateAcrylicGlowColor()
    {
        InternalLogger.Debug(Tag, () => "UpdateAcrylicGlowColor()");
        _acrylicLayer?.SetColor(MaterialFrame.AcrylicGlowColor.ToPlatform());
    }

    private void UpdateMaterialTheme()
    {
        InternalLogger.Debug(Tag, () => "UpdateMaterialTheme()");

        switch (MaterialFrame.MaterialTheme)
        {
            case MaterialFrame.Theme.Acrylic:
                SetAcrylicTheme();
                break;

            case MaterialFrame.Theme.Dark:
                SetDarkTheme();
                break;

            case MaterialFrame.Theme.Light:
                SetLightTheme();
                break;

            case MaterialFrame.Theme.AcrylicBlur:
                SetAcrylicBlurTheme();
                break;
        }

        PlatformView.Invalidate();
    }

    private void SetAcrylicBlurTheme()
    {
        InternalLogger.Debug(Tag, () => "SetAcrylicBlurTheme()");

        _mainDrawable = new GradientDrawable();
        _mainDrawable.SetColor(Colors.Transparent.ToPlatform());

        PlatformView.SetBackground(_mainDrawable);

        UpdateCornerRadius();
        UpdateElevation();

        EnableBlur();
    }

    private void SetDarkTheme()
    {
        InternalLogger.Debug(Tag, () => "SetDarkTheme()");
        DisableBlur();

        _mainDrawable = new GradientDrawable();
        _mainDrawable.SetColor(MaterialFrame.ElevationToColor().ToPlatform());

        PlatformView.SetBackground(_mainDrawable);

        UpdateCornerRadius();
        UpdateElevation();
    }

    private void SetLightTheme()
    {
        InternalLogger.Debug(Tag, () => "SetLightTheme()");
        DisableBlur();

        _mainDrawable = new GradientDrawable();
        _mainDrawable.SetColor(MaterialFrame.LightThemeBackgroundColor.ToPlatform());

        PlatformView.SetBackground(_mainDrawable);

        UpdateCornerRadius();
        UpdateElevation();
    }

    private void SetAcrylicTheme()
    {
        InternalLogger.Debug(Tag, () => "SetAcrylicTheme()");
        DisableBlur();

        if (_acrylicLayer == null)
        {
            _acrylicLayer = new GradientDrawable();
            _acrylicLayer.SetShape(ShapeType.Rectangle);
        }

        UpdateAcrylicGlowColor();
        UpdateCornerRadius();

        _mainDrawable.SetColor(MaterialFrame.LightThemeBackgroundColor.ToPlatform());

        LayerDrawable layer = new LayerDrawable([_acrylicLayer, _mainDrawable]);
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
        {
            layer.SetLayerInsetTop(1, (int)Context.ToPixels(2));
        }
        else
        {
            Console.WriteLine(
                $"{DateTime.Now:MM-dd H:mm:ss.fff} | Sharpnado.MaterialFrame | WARNING | The Acrylic glow is only supported on android API 23 or greater (starting from Marshmallow)");
        }

        PlatformView.SetBackground(layer);

        UpdateElevation();
    }
}
