using System.Numerics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Shapes;

using AcrylicBrush = Microsoft.UI.Xaml.Media.AcrylicBrush;
using CornerRadius = Microsoft.UI.Xaml.CornerRadius;
using Grid = Microsoft.UI.Xaml.Controls.Grid;
using Thickness = Microsoft.UI.Xaml.Thickness;

namespace Sharpnado.MaterialFrame.WinUI;

public class WinUIMaterialFrameHandler : ViewHandler<MaterialFrame, Grid>
{
    public static IPropertyMapper<MaterialFrame, WinUIMaterialFrameHandler> MaterialFrameMapper = new PropertyMapper<MaterialFrame, WinUIMaterialFrameHandler>(ViewHandler.ViewMapper)
    {
        [nameof(MaterialFrame.CornerRadius)] = MapCornerRadius,
        [nameof(MaterialFrame.Elevation)] = MapElevation,
        [nameof(MaterialFrame.LightThemeBackgroundColor)] = MapLightThemeBackgroundColor,
        [nameof(MaterialFrame.AcrylicGlowColor)] = MapAcrylicGlowColor,
        [nameof(MaterialFrame.MaterialTheme)] = MapMaterialTheme,
        [nameof(MaterialFrame.MaterialBlurStyle)] = MapMaterialBlurStyle,
        [nameof(MaterialFrame.Padding)] = MapMarginAndPadding,
        [nameof(MaterialFrame.Margin)] = MapMarginAndPadding,
        [nameof(MaterialFrame.WinUIBlurOverlayColor)] = MapBlur,
    };

    public WinUIMaterialFrameHandler() : base(MaterialFrameMapper)
    {
    }

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
    private FrameworkElement? _contentElement;

    private Compositor? _compositor;
    private SpriteVisual? _shadowVisual;

    // Static mapper methods
    public static void MapCornerRadius(WinUIMaterialFrameHandler handler, MaterialFrame view) => handler.UpdateCornerRadius();
    public static void MapElevation(WinUIMaterialFrameHandler handler, MaterialFrame view) => handler.UpdateElevation();
    public static void MapLightThemeBackgroundColor(WinUIMaterialFrameHandler handler, MaterialFrame view) => handler.UpdateLightThemeBackgroundColor();
    public static void MapAcrylicGlowColor(WinUIMaterialFrameHandler handler, MaterialFrame view) => handler.UpdateAcrylicGlowColor();
    public static void MapMaterialTheme(WinUIMaterialFrameHandler handler, MaterialFrame view) => handler.UpdateMaterialTheme();
    public static void MapMaterialBlurStyle(WinUIMaterialFrameHandler handler, MaterialFrame view) => handler.UpdateMaterialBlurStyle();
    public static void MapMarginAndPadding(WinUIMaterialFrameHandler handler, MaterialFrame view) => handler.UpdateMarginAndPadding();
    public static void MapBlur(WinUIMaterialFrameHandler handler, MaterialFrame view) => handler.UpdateBlur();

    protected override Grid CreatePlatformView()
    {
        InternalLogger.Debug(Tag, () => "CreatePlatformView()");

        var rootGrid = new Grid();
        
        _acrylicRectangle = new Rectangle();
        _shadowHost = new Rectangle
        {
            Fill = Colors.Transparent.ToPlatform(),
        };

        _acrylicGrid = new Grid();
        _acrylicGrid.Children.Add(_acrylicRectangle);

        rootGrid.Children.Add(_shadowHost);
        rootGrid.Children.Add(_acrylicGrid);

        rootGrid.SizeChanged += OnSizeChanged;

        return rootGrid;
    }

    protected override void ConnectHandler(Grid platformView)
    {
        base.ConnectHandler(platformView);

        InternalLogger.Debug(Tag, () => "ConnectHandler()");

        // Add the content view to the grid
        if (VirtualView?.Content != null)
        {
            _contentElement = VirtualView.Content.ToPlatform(MauiContext!);
            if (_contentElement != null)
            {
                platformView.Children.Add(_contentElement);
            }
        }

        UpdateCornerRadius();
        UpdateMarginAndPadding();
        UpdateMaterialTheme();
    }

    protected override void DisconnectHandler(Grid platformView)
    {
        InternalLogger.Debug(Tag, () => "DisconnectHandler()");

        VirtualView.Unsubscribe();

        platformView.SizeChanged -= OnSizeChanged;
            
        if (_contentElement != null)
        {
            platformView.Children.Remove(_contentElement);
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
        _contentElement = null;

        base.DisconnectHandler(platformView);
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        InternalLogger.Debug(Tag, () => $"OnSizeChanged: w: {e.NewSize.Width}, h: {e.NewSize.Height}");
        UpdateElevation();
    }

    private void UpdateMarginAndPadding()
    {
        if (_contentElement == null)
        {
            return;
        }

        _contentElement.Margin = VirtualView.Padding.ToPlatform();
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
            _acrylicRectangle.Fill = Colors.Transparent.ToPlatform();
        }
    }

    private void UpdateCornerRadius()
    {
        if (_acrylicGrid == null || _shadowHost == null || _acrylicRectangle == null)
        {
            return;
        }

        float cornerRadius = VirtualView.CornerRadius;

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
        if (_acrylicGrid == null || _acrylicRectangle == null)
        {
            return;
        }

        switch (VirtualView.MaterialTheme)
        {
            case MaterialFrame.Theme.Acrylic:
                _acrylicRectangle.Fill = VirtualView.LightThemeBackgroundColor.ToPlatform();
                break;

            case MaterialFrame.Theme.AcrylicBlur:
            case MaterialFrame.Theme.Dark:
                return;

            case MaterialFrame.Theme.Light:
                _acrylicGrid.Background = VirtualView.LightThemeBackgroundColor.ToPlatform();
                break;
        }
    }

    private void UpdateAcrylicGlowColor()
    {
        if (_acrylicGrid == null || VirtualView.MaterialTheme != MaterialFrame.Theme.Acrylic)
        {
            return;
        }

        _acrylicGrid.Background = VirtualView.AcrylicGlowColor.ToPlatform();
    }

    private void UpdateElevation()
    {
        if (_acrylicGrid == null || _shadowHost == null)
        {
            return;
        }

        if (!VirtualView.IsShadowCompatible)
        {
            _shadowHost.Fill = Colors.Transparent.ToPlatform();

            if (_shadowVisual != null)
            {
                ElementCompositionPreview.SetElementChildVisual(_shadowHost, null);
                _shadowVisual.Dispose();
                _shadowVisual = null;
            }

            if (VirtualView.MaterialTheme == MaterialFrame.Theme.Dark)
            {
                _acrylicGrid.Background = VirtualView.ElevationToColor().ToPlatform();
            }

            return;
        }

        if (VirtualView.Width < 1 || VirtualView.Height < 1)
        {
            return;
        }

        // https://docs.microsoft.com/en-US/windows/uwp/composition/using-the-visual-layer-with-xaml
        bool isAcrylicTheme = VirtualView.MaterialTheme == MaterialFrame.Theme.Acrylic;

        float blurRadius = isAcrylicTheme ? MaterialFrame.AcrylicElevation : VirtualView.Elevation;
        int elevation = isAcrylicTheme ? MaterialFrame.AcrylicElevation / 3 : VirtualView.Elevation / 2;
        float opacity = isAcrylicTheme ? 0.12f : 0.16f;

        int width = (int)VirtualView.Width;
        int height = (int)VirtualView.Height;

        _shadowHost.Fill = Colors.White.ToPlatform();
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
        if (_acrylicGrid == null)
        {
            return;
        }

        switch (VirtualView.MaterialTheme)
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
        if (_acrylicGrid == null || VirtualView.MaterialTheme != MaterialFrame.Theme.AcrylicBlur)
        {
            return;
        }

        var acrylicBrush = new AcrylicBrush();

        // Background acrylic isn't currently supported. The mentioned property was removed in .8 release: https://github.com/microsoft/microsoft-ui-xaml/issues/6618
        // { BackgroundSource = VirtualView.WinUIHostBackdropBlur ? AcrylicBackgroundSource.HostBackdrop : AcrylicBackgroundSource.Backdrop };

        switch (VirtualView.MaterialBlurStyle)
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
        if (_acrylicGrid == null)
        {
            return;
        }

        if (VirtualView.WinUIBlurOverlayColor != Colors.Transparent)
        {
            var acrylicBrush = new AcrylicBrush
            {
                // Background acrylic isn't currently supported.
                // BackgroundSource = VirtualView.WinUIHostBackdropBlur ? AcrylicBackgroundSource.HostBackdrop : AcrylicBackgroundSource.Backdrop,
                TintColor = VirtualView.WinUIBlurOverlayColor.ToWindowsColor(),
            };

            _acrylicGrid.Background = acrylicBrush;
            return;
        }

        UpdateMaterialBlurStyle();
    }
}
