using System.Numerics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Shapes;
using FrameworkElement = Microsoft.UI.Xaml.FrameworkElement;

using AcrylicBrush = Microsoft.UI.Xaml.Media.AcrylicBrush;
using Brush = Microsoft.UI.Xaml.Media.Brush;
using CornerRadius = Microsoft.UI.Xaml.CornerRadius;
using Grid = Microsoft.UI.Xaml.Controls.Grid;
using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using Thickness = Microsoft.UI.Xaml.Thickness;
using ContentView = Microsoft.Maui.Platform.ContentPanel;

namespace Sharpnado.MaterialFrame.WinUI;

public class WinUIMaterialFrameHandler() : ContentViewHandler(MaterialFrameMapper)
{
    public static PropertyMapper<MaterialFrame, WinUIMaterialFrameHandler> MaterialFrameMapper = new(Mapper)
    {
        [nameof(MaterialFrame.CornerRadius)] = (handler, view) => handler.UpdateCornerRadius(),
        [nameof(MaterialFrame.Elevation)] = (handler, view) => handler.UpdateElevation(),
        [nameof(MaterialFrame.LightThemeBackgroundColor)] = (handler, view) => handler.UpdateLightThemeBackgroundColor(),
        [nameof(MaterialFrame.AcrylicGlowColor)] = (handler, view) => handler.UpdateAcrylicGlowColor(),
        [nameof(MaterialFrame.MaterialTheme)] = (handler, view) => handler.UpdateMaterialTheme(),
        [nameof(MaterialFrame.MaterialBlurStyle)] = (handler, view) => handler.UpdateMaterialBlurStyle(),
        [nameof(MaterialFrame.BorderColor)] = (handler, view) => handler.UpdateBorder(),
        [nameof(MaterialFrame.Padding)] = (handler, view) => handler.UpdateMarginAndPadding(),
        [nameof(MaterialFrame.Margin)] = (handler, view) => handler.UpdateMarginAndPadding(),
        [nameof(MaterialFrame.WinUIBlurOverlayColor)] = (handler, view) => handler.UpdateBlur(),
    };

    private const string Tag = nameof(WinUIMaterialFrameHandler);

    private static readonly Color DarkBlurOverlayColor = Color.FromHex("#80000000");
    private static readonly Color DarkFallBackColor = Color.FromHex("#333333");

    private static readonly Color LightBlurOverlayColor = Color.FromHex("#40FFFFFF");
    private static readonly Color LightFallBackColor = Color.FromHex("#F3F3F3");

    private static readonly Color ExtraLightBlurOverlayColor = Color.FromHex("#B0FFFFFF");
    private static readonly Color ExtraLightFallBackColor = Color.FromHex("#FBFBFB");

    private Rectangle? _acrylicRectangle;
    private Rectangle? _shadowHost;
    private Grid? _acrylicGrid;
    private Grid? _rootGrid;

    private Compositor? _compositor;
    private SpriteVisual? _shadowVisual;

    private MaterialFrame MaterialFrame => (MaterialFrame)VirtualView;

    protected override ContentView CreatePlatformView()
    {
        InternalLogger.Debug(Tag, () => "CreatePlatformView()");

        var contentView = base.CreatePlatformView();
        
        _rootGrid = new Grid();
        
        _acrylicRectangle = new Rectangle();
        _shadowHost = new Rectangle
        {
            Fill = Colors.Transparent.ToBrush(),
        };

        _acrylicGrid = new Grid();
        _acrylicGrid.Children.Add(_acrylicRectangle);

        _rootGrid.Children.Add(_shadowHost);
        _rootGrid.Children.Add(_acrylicGrid);
        _rootGrid.Children.Add(contentView);

        _rootGrid.SizeChanged += OnSizeChanged;

        return contentView;
    }

    protected override FrameworkElement ContainerView => _rootGrid ?? base.ContainerView;

    protected override void ConnectHandler(ContentView platformView)
    {
        base.ConnectHandler(platformView);

        InternalLogger.Debug(Tag, () => "ConnectHandler()");

        UpdateBorder();
        UpdateCornerRadius();
        UpdateMarginAndPadding();
        UpdateMaterialTheme();
    }

    protected override void DisconnectHandler(ContentView platformView)
    {
        InternalLogger.Debug(Tag, () => "DisconnectHandler()");

        MaterialFrame?.Unsubscribe();

        if (_rootGrid != null)
        {
            _rootGrid.SizeChanged -= OnSizeChanged;
        }

        if (_shadowVisual != null)
        {
            if (_shadowHost != null)
            {
                ElementCompositionPreview.SetElementChildVisual(_shadowHost, null);
            }
            _shadowVisual.Dispose();
            _shadowVisual = null;
        }

        _compositor = null;
        _acrylicRectangle = null;
        _shadowHost = null;
        _acrylicGrid = null;
        _rootGrid = null;

        base.DisconnectHandler(platformView);
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        InternalLogger.Debug(Tag, () => $"OnSizeChanged: w: {e.NewSize.Width}, h: {e.NewSize.Height}");
        UpdateElevation();
    }

    private void UpdateMarginAndPadding()
    {
        if (PlatformView == null || MaterialFrame == null)
        {
            return;
        }

        PlatformView.Margin = MaterialFrame.Padding.ToPlatform();
    }

    private void ToggleAcrylicRectangle(bool enable)
    {
        if (_acrylicRectangle == null)
        {
            return;
        }

        _acrylicRectangle.Margin = new Thickness(0, enable ? 2 : 0, 0, 0);
        if (!enable)
        {
            _acrylicRectangle.Fill = Colors.Transparent.ToBrush();
        }
    }

    private void UpdateBorder()
    {
        if (_acrylicGrid == null)
        {
            return;
        }

        if (MaterialFrame?.BorderColor != null)
        {
            _acrylicGrid.BorderBrush = MaterialFrame.BorderColor.ToBrush();
            _acrylicGrid.BorderThickness = new Thickness(1);
        }
        else
        {
            _acrylicGrid.BorderBrush = new Color(0, 0, 0, 0).ToBrush();
        }
    }

    private void UpdateCornerRadius()
    {
        if (_acrylicGrid == null || _shadowHost == null || _acrylicRectangle == null)
        {
            return;
        }

        float cornerRadius = MaterialFrame?.CornerRadius ?? -1;

        if (cornerRadius == -1f)
        {
            cornerRadius = 5f; // default corner radius
        }

        _acrylicGrid.CornerRadius = new CornerRadius(cornerRadius);
        _shadowHost.RadiusX = cornerRadius;
        _shadowHost.RadiusY = cornerRadius;
        _acrylicRectangle.RadiusX = cornerRadius;
        _acrylicRectangle.RadiusY = cornerRadius;
    }

    private void UpdateLightThemeBackgroundColor()
    {
        if (_acrylicGrid == null || _acrylicRectangle == null || MaterialFrame == null)
        {
            return;
        }

        switch (MaterialFrame.MaterialTheme)
        {
            case MaterialFrame.Theme.Acrylic:
                _acrylicRectangle.Fill = MaterialFrame.LightThemeBackgroundColor.ToBrush();
                break;

            case MaterialFrame.Theme.AcrylicBlur:
            case MaterialFrame.Theme.Dark:
                return;

            case MaterialFrame.Theme.Light:
                _acrylicGrid.Background = MaterialFrame.LightThemeBackgroundColor.ToBrush();
                break;
        }
    }

    private void UpdateAcrylicGlowColor()
    {
        if (_acrylicGrid == null || MaterialFrame == null || MaterialFrame.MaterialTheme != MaterialFrame.Theme.Acrylic)
        {
            return;
        }

        _acrylicGrid.Background = MaterialFrame.AcrylicGlowColor.ToBrush();
    }

    private void UpdateElevation()
    {
        if (_acrylicGrid == null || _shadowHost == null || MaterialFrame == null)
        {
            return;
        }

        if (!MaterialFrame.IsShadowCompatible)
        {
            _shadowHost.Fill = Colors.Transparent.ToBrush();

            if (_shadowVisual != null)
            {
                ElementCompositionPreview.SetElementChildVisual(_shadowHost, null);
                _shadowVisual.Dispose();
                _shadowVisual = null;
            }

            if (MaterialFrame.MaterialTheme == MaterialFrame.Theme.Dark)
            {
                _acrylicGrid.Background = MaterialFrame.ElevationToColor().ToBrush();
            }

            return;
        }

        if (MaterialFrame.Width < 1 || MaterialFrame.Height < 1)
        {
            return;
        }

        // https://docs.microsoft.com/en-US/windows/uwp/composition/using-the-visual-layer-with-xaml
        bool isAcrylicTheme = MaterialFrame.MaterialTheme == MaterialFrame.Theme.Acrylic;

        float blurRadius = isAcrylicTheme ? MaterialFrame.AcrylicElevation : MaterialFrame.Elevation;
        int elevation = isAcrylicTheme ? MaterialFrame.AcrylicElevation / 3 : MaterialFrame.Elevation / 2;
        float opacity = isAcrylicTheme ? 0.12f : 0.16f;

        int width = (int)MaterialFrame.Width;
        int height = (int)MaterialFrame.Height;

        _shadowHost.Fill = Colors.White.ToBrush();
        _shadowHost.Width = width;
        _shadowHost.Height = height;

        if (_compositor == null)
        {
            Visual hostVisual = ElementCompositionPreview.GetElementVisual(_acrylicGrid);
            _compositor = hostVisual.Compositor;
        }

        var dropShadow = _compositor.CreateDropShadow();
        dropShadow.BlurRadius = blurRadius;
        dropShadow.Opacity = opacity;
        dropShadow.Color = Colors.Black.ToWindowsColor();
        dropShadow.Offset = new Vector3(0, elevation, 0);
        dropShadow.Mask = _shadowHost.GetAlphaMask();

        _shadowVisual = _compositor.CreateSpriteVisual();
        _shadowVisual.Size = new Vector2(width, height);
        _shadowVisual.Shadow = dropShadow;

        ElementCompositionPreview.SetElementChildVisual(_shadowHost, _shadowVisual);
    }

    private void UpdateMaterialTheme()
    {
        if (_acrylicGrid == null || MaterialFrame == null)
        {
            return;
        }

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

        UpdateBorder();
        UpdateCornerRadius();
        UpdateElevation();
    }

    private void SetDarkTheme()
    {
        ToggleAcrylicRectangle(false);
    }

    private void SetLightTheme()
    {
        ToggleAcrylicRectangle(false);
        UpdateLightThemeBackgroundColor();
    }

    private void SetAcrylicTheme()
    {
        ToggleAcrylicRectangle(true);
        UpdateLightThemeBackgroundColor();
        UpdateAcrylicGlowColor();
    }

    private void SetAcrylicBlurTheme()
    {
        ToggleAcrylicRectangle(false);
        UpdateBlur();
    }

    private void UpdateMaterialBlurStyle()
    {
        if (_acrylicGrid == null || MaterialFrame == null || MaterialFrame.MaterialTheme != MaterialFrame.Theme.AcrylicBlur)
        {
            return;
        }

        var acrylicBrush = new AcrylicBrush();

        // Background acrylic isn't currently supported. The mentioned property was removed in .8 release: https://github.com/microsoft/microsoft-ui-xaml/issues/6618
        // { BackgroundSource = MaterialFrame.WinUIHostBackdropBlur ? AcrylicBackgroundSource.HostBackdrop : AcrylicBackgroundSource.Backdrop };

        switch (MaterialFrame.MaterialBlurStyle)
        {
            case MaterialFrame.BlurStyle.ExtraLight:
                acrylicBrush.TintColor = ExtraLightBlurOverlayColor.ToWindowsColor();
                acrylicBrush.FallbackColor = ExtraLightFallBackColor.ToWindowsColor();
                break;
            case MaterialFrame.BlurStyle.Dark:
                acrylicBrush.TintColor = DarkBlurOverlayColor.ToWindowsColor();
                acrylicBrush.FallbackColor = DarkFallBackColor.ToWindowsColor();
                break;
            default:
                acrylicBrush.TintColor = LightBlurOverlayColor.ToWindowsColor();
                acrylicBrush.FallbackColor = LightFallBackColor.ToWindowsColor();
                break;
        }

        _acrylicGrid.Background = acrylicBrush;
    }

    private void UpdateBlur()
    {
        if (_acrylicGrid == null || MaterialFrame == null)
        {
            return;
        }

        if (MaterialFrame.WinUIBlurOverlayColor != Colors.Transparent)
        {
            var acrylicBrush = new AcrylicBrush
            {
                // Background acrylic isn't currently supported.
                // BackgroundSource = MaterialFrame.WinUIHostBackdropBlur ? AcrylicBackgroundSource.HostBackdrop : AcrylicBackgroundSource.Backdrop,
                TintColor = MaterialFrame.WinUIBlurOverlayColor.ToWindowsColor(),
            };

            _acrylicGrid.Background = acrylicBrush;
            return;
        }

        UpdateMaterialBlurStyle();
    }
}

internal static class ColorExtensions
{
    public static Brush ToBrush(this Color color)
    {
        return new SolidColorBrush(color.ToWindowsColor());
    }
}
