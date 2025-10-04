using CoreAnimation;

using CoreGraphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

using UIKit;
using ContentView = Microsoft.Maui.Platform.ContentView;

namespace Sharpnado.MaterialFrame.iOS;

public class iOSMaterialFrameHandler() : ContentViewHandler(MaterialFrameMapper)
{
    public static PropertyMapper<MaterialFrame, iOSMaterialFrameHandler> MaterialFrameMapper = new(Mapper)
    {
        [nameof(MaterialFrame.CornerRadius)] = (handler, view) => handler.UpdateCornerRadius(),
        [nameof(MaterialFrame.Elevation)] = (handler, view) => handler.UpdateElevation(),
        [nameof(MaterialFrame.LightThemeBackgroundColor)] = (handler, view) => handler.UpdateLightThemeBackgroundColor(),
        [nameof(MaterialFrame.AcrylicGlowColor)] = (handler, view) => handler.UpdateAcrylicGlowColor(),
        [nameof(MaterialFrame.MaterialTheme)] = (handler, view) => handler.UpdateMaterialTheme(),
        [nameof(MaterialFrame.MaterialBlurStyle)] = (handler, view) => handler.UpdateMaterialBlurStyle(),
    };

    private const string Tag = nameof(iOSMaterialFrameHandler);

    private CALayer? _intermediateLayer;

    private UIVisualEffectView? _blurView;

    private MaterialFrame MaterialFrame => (MaterialFrame)VirtualView;

    public override void PlatformArrange(Rect rect)
    {
        base.PlatformArrange(rect);

        InternalLogger.Debug(Tag, () => "PlatformArrange()");

        UpdateShadowPath();
        UpdateLayerBounds();

        if (MaterialFrame.MaterialTheme == MaterialFrame.Theme.AcrylicBlur)
        {
            EnableBlur();
        }
    }

    protected override ContentView CreatePlatformView()
    {
        InternalLogger.Debug(Tag, () => "CreatePlatformView()");

        var contentView = base.CreatePlatformView();

        return contentView;
    }

    protected override void ConnectHandler(ContentView platformView)
    {
        base.ConnectHandler(platformView);

        InternalLogger.Debug(Tag, () => "ConnectHandler()");

        _intermediateLayer = new CALayer { BackgroundColor = Colors.Transparent.ToCGColor() };
        platformView.Layer.InsertSublayer(_intermediateLayer, 0);

        UpdateMaterialTheme();
    }

    protected override void DisconnectHandler(ContentView platformView)
    {
        InternalLogger.Debug(Tag, () => "DisconnectHandler()");

        MaterialFrame?.Unsubscribe();

        _intermediateLayer?.RemoveFromSuperLayer();
        _intermediateLayer?.Dispose();
        _intermediateLayer = null;

        _blurView?.RemoveFromSuperview();
        _blurView?.Dispose();
        _blurView = null;

        base.DisconnectHandler(platformView);
    }

    private void UpdateShadowPath()
    {
        var layer = PlatformView.Layer;
        if (layer.Bounds.Width > 0 && layer.ShadowRadius > 0)
        {
            float radius = MaterialFrame?.CornerRadius ?? -1;
            if (radius == -1.0f)
            {
                radius = 5f;
            }

            layer.ShadowPath = UIBezierPath.FromRoundedRect(layer.Bounds, radius).CGPath;
        }
    }

    private static bool SizeIsEqual(CGRect frame, MaterialFrame element)
    {
        return frame.Width == element.Width && frame.Height == element.Height;
    }

    private void UpdateLightThemeBackgroundColor()
    {
        InternalLogger.Debug(Tag, () => $"UpdateLightThemeBackgroundColor() => LightThemeBackgroundColor: {MaterialFrame.LightThemeBackgroundColor}");
        switch (MaterialFrame.MaterialTheme)
        {
            case MaterialFrame.Theme.Acrylic:
                if (_intermediateLayer != null)
                {
                    _intermediateLayer.BackgroundColor = MaterialFrame.LightThemeBackgroundColor.ToCGColor();
                }

                break;

            case MaterialFrame.Theme.AcrylicBlur:
            case MaterialFrame.Theme.Dark:
                return;

            case MaterialFrame.Theme.Light:
                PlatformView.Layer.BackgroundColor = MaterialFrame.LightThemeBackgroundColor.ToCGColor();
                break;
        }
    }

    private void UpdateAcrylicGlowColor()
    {
        if (MaterialFrame.MaterialTheme != MaterialFrame.Theme.Acrylic)
        {
            return;
        }

        InternalLogger.Debug(Tag, () => "UpdateAcrylicGlowColor()");
        PlatformView.Layer.BackgroundColor = MaterialFrame.AcrylicGlowColor.ToCGColor();
    }

    private void UpdateElevation()
    {
        InternalLogger.Debug(Tag, () => "UpdateElevation()");

        var layer = PlatformView.Layer;

        if (MaterialFrame.MaterialTheme == MaterialFrame.Theme.AcrylicBlur)
        {
            layer.ShadowOpacity = 0.0f;
            return;
        }

        if (MaterialFrame.MaterialTheme == MaterialFrame.Theme.Dark)
        {
            layer.ShadowOpacity = 0.0f;
            layer.BackgroundColor = MaterialFrame.ElevationToColor().ToCGColor();
            return;
        }

        bool isAcrylicTheme = MaterialFrame.MaterialTheme == MaterialFrame.Theme.Acrylic;

        float adaptedElevation = isAcrylicTheme ? MaterialFrame.AcrylicElevation / 3 : MaterialFrame.Elevation / 2;
        float opacity = isAcrylicTheme ? 0.12f : 0.24f;

        layer.ShadowColor = UIColor.Black.CGColor;
        layer.ShadowRadius = Math.Abs(adaptedElevation);
        layer.ShadowOffset = new CGSize(0, adaptedElevation);
        layer.ShadowOpacity = opacity;

        layer.MasksToBounds = false;

        layer.RasterizationScale = UIScreen.MainScreen.Scale;
        layer.ShouldRasterize = true;
    }

    private void UpdateCornerRadius()
    {
        InternalLogger.Debug(Tag, () => "UpdateCornerRadius()");

        float radius = MaterialFrame.CornerRadius;
        if (radius == -1.0f)
        {
            radius = 5f;
        }

        PlatformView.Layer.CornerRadius = radius;
        if (_intermediateLayer != null)
        {
            _intermediateLayer.CornerRadius = radius;
        }

        if (_blurView != null)
        {
            _blurView.Layer.CornerRadius = radius;
        }
    }

    private void UpdateMaterialTheme()
    {
        InternalLogger.Debug(Tag, () => $"UpdateMaterialTheme() => MaterialTheme: {MaterialFrame.MaterialTheme}");
        switch (MaterialFrame.MaterialTheme)
        {
            case MaterialFrame.Theme.Acrylic:
                SetAcrylicTheme();
                break;

            case MaterialFrame.Theme.AcrylicBlur:
                SetAcrylicBlurTheme();
                break;

            case MaterialFrame.Theme.Dark:
                SetDarkTheme();
                break;

            case MaterialFrame.Theme.Light:
                SetLightTheme();
                break;
        }
    }

    private void SetDarkTheme()
    {
        InternalLogger.Debug(Tag, () => "SetDarkTheme()");

        if (_intermediateLayer != null)
        {
            _intermediateLayer.BackgroundColor = Colors.Transparent.ToCGColor();
        }

        PlatformView.Layer.BackgroundColor = MaterialFrame.ElevationToColor().ToCGColor();

        UpdateCornerRadius();
        UpdateElevation();

        DisableBlur();
    }

    private void SetLightTheme()
    {
        InternalLogger.Debug(Tag, () => "SetLightTheme()");

        if (_intermediateLayer != null)
        {
            _intermediateLayer.BackgroundColor = Colors.Transparent.ToCGColor();
        }

        PlatformView.Layer.BackgroundColor = MaterialFrame.LightThemeBackgroundColor.ToCGColor();

        UpdateCornerRadius();
        UpdateElevation();

        DisableBlur();
    }

    private void SetAcrylicTheme()
    {
        InternalLogger.Debug(Tag, () => "SetAcrylicTheme()");

        if (_intermediateLayer != null)
        {
            _intermediateLayer.BackgroundColor = MaterialFrame.LightThemeBackgroundColor.ToCGColor();
        }

        UpdateAcrylicGlowColor();

        UpdateCornerRadius();
        UpdateElevation();

        DisableBlur();
    }

    private void SetAcrylicBlurTheme()
    {
        InternalLogger.Debug(Tag, () => "SetAcrylicBlurTheme()");

        if (_intermediateLayer != null)
        {
            _intermediateLayer.BackgroundColor = Colors.Transparent.ToCGColor();
        }
            
        PlatformView.Layer.BackgroundColor = Colors.Transparent.ToCGColor();

        EnableBlur();

        UpdateCornerRadius();
        UpdateElevation();
    }

    private void UpdateMaterialBlurStyle()
    {
        InternalLogger.Debug(Tag, () => "UpdateMaterialBlurStyle()");

        if (_blurView != null)
        {
            _blurView.Effect = UIBlurEffect.FromStyle(ConvertBlurStyle());
        }
    }

    private void EnableBlur()
    {
        InternalLogger.Debug(Tag, () => "EnableBlur()");

        if (_blurView == null)
        {
            _blurView = new UIVisualEffectView()
            {
                ClipsToBounds = true,
                BackgroundColor = UIColor.Clear,
                TranslatesAutoresizingMaskIntoConstraints = false,
            };
        }

        if (PlatformView.Subviews.Length > 0 && ReferenceEquals(PlatformView.Subviews[0], _blurView))
        {
            return;
        }

        UpdateMaterialBlurStyle();

        PlatformView.InsertSubview(_blurView, 0);

        NSLayoutConstraint.ActivateConstraints(new[]
        {
            _blurView.LeadingAnchor.ConstraintEqualTo(PlatformView.LeadingAnchor),
            _blurView.TopAnchor.ConstraintEqualTo(PlatformView.TopAnchor),
            _blurView.WidthAnchor.ConstraintEqualTo(PlatformView.WidthAnchor),
            _blurView.HeightAnchor.ConstraintEqualTo(PlatformView.HeightAnchor),
        });
    }

    private void DisableBlur()
    {
        InternalLogger.Debug(Tag, () => "DisableBlur()");

        _blurView?.RemoveFromSuperview();
    }

    private void UpdateLayerBounds()
    {
        if (_intermediateLayer == null)
        {
            return;
        }

        if (MaterialFrame.Width > 0 && MaterialFrame.Height > 0 && !SizeIsEqual(_intermediateLayer.Frame, MaterialFrame))
        {
            InternalLogger.Debug(Tag, () => "UpdateLayerBounds()");

            _intermediateLayer.Frame = new CGRect(0, 2, MaterialFrame.Width, MaterialFrame.Height - 2);
            _intermediateLayer.RemoveAllAnimations();
        }
    }

    private void UpdateBlurViewBounds()
    {
        if (_blurView == null)
        {
            return;
        }

        if (_blurView != null && MaterialFrame.Width > 0 && MaterialFrame.Height > 0 && !SizeIsEqual(_blurView.Frame, MaterialFrame))
        {
            InternalLogger.Debug(Tag, () => "UpdateBlurViewBounds()");

            _blurView.Frame = new CGRect(0, 0, MaterialFrame.Width, MaterialFrame.Height);
        }
    }

    private UIBlurEffectStyle ConvertBlurStyle()
    {
        switch (MaterialFrame.MaterialBlurStyle)
        {
            case MaterialFrame.BlurStyle.ExtraLight:
                return UIBlurEffectStyle.ExtraLight;
            case MaterialFrame.BlurStyle.Dark:
                return UIBlurEffectStyle.Dark;

            default:
                return UIBlurEffectStyle.Light;
        }
    }
}
