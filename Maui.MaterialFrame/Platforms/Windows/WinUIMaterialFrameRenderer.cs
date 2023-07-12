using System.ComponentModel;
using System.Numerics;

using Microsoft.Maui.Controls.Compatibility.Platform.UWP;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Platform;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

using AcrylicBrush = Microsoft.UI.Xaml.Media.AcrylicBrush;
using Brush = Microsoft.UI.Xaml.Media.Brush;
using CornerRadius = Microsoft.UI.Xaml.CornerRadius;
using Grid = Microsoft.UI.Xaml.Controls.Grid;
using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using Thickness = Microsoft.UI.Xaml.Thickness;

namespace Sharpnado.MaterialFrame.WinUI
{
    /// <summary>
    ///     Renderer to update all frames with better shadows matching material design standards.
    /// </summary>
    public class WinUIMaterialFrameRenderer : Microsoft.Maui.Controls.Handlers.Compatibility.ViewRenderer<MaterialFrame, Grid>
    {
        private static readonly Color DarkBlurOverlayColor = Color.FromHex("#80000000");
        private static readonly Color DarkFallBackColor = Color.FromHex("#333333");

        private static readonly Color LightBlurOverlayColor = Color.FromHex("#40FFFFFF");
        private static readonly Color LightFallBackColor = Color.FromHex("#F3F3F3");

        private static readonly Color ExtraLightBlurOverlayColor = Color.FromHex("#B0FFFFFF");
        private static readonly Color ExtraLightFallBackColor = Color.FromHex("#FBFBFB");

        private Rectangle _acrylicRectangle;
        private Rectangle _shadowHost;
        private Grid _grid;

        private Compositor _compositor;
        private SpriteVisual _shadowVisual;

        public WinUIMaterialFrameRenderer()
        {
            AutoPackage = false;
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            // We need an automation peer so we can interact with this in automated tests
            if (Control == null)
            {
                return new FrameworkElementAutomationPeer(this);
            }

            return new FrameworkElementAutomationPeer(Control);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
            }
        }

        protected override void OnElementChanged(ElementChangedEventArgs<MaterialFrame> e)
        {
            base.OnElementChanged(e);

            e.OldElement?.Unsubscribe();

            if (e.NewElement == null)
            {
                return;
            }

            var grid = new Grid
                {
                    Margin = Element.Margin.ToPlatform(),
                    Padding = Element.Padding.ToPlatform(),
                };

            if (Control == null)
            {
                SetNativeControl(grid);
            }

            PackChild();
            UpdateBorder();
            UpdateCornerRadius();
            UpdateMaterialTheme();
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MaterialFrame.Padding):
                case nameof(MaterialFrame.Margin):
                    //UpdateMarginAndPadding();
                    break;

                case nameof(MaterialFrame.BorderColor):
                    UpdateBorder();
                    break;

                case nameof(MaterialFrame.CornerRadius):
                    UpdateCornerRadius();
                    break;

                case nameof(MaterialFrame.Width):
                case nameof(MaterialFrame.Height):
                case nameof(MaterialFrame.Elevation):
                    UpdateElevation();
                    break;

                case nameof(MaterialFrame.LightThemeBackgroundColor):
                    UpdateLightThemeBackgroundColor();
                    break;

                case nameof(MaterialFrame.AcrylicGlowColor):
                    UpdateAcrylicGlowColor();
                    break;

                case nameof(MaterialFrame.MaterialTheme):
                    UpdateMaterialTheme();
                    break;

                case nameof(MaterialFrame.WinUIBlurOverlayColor):
                    UpdateBlur();
                    break;

                case nameof(MaterialFrame.MaterialBlurStyle):
                    UpdateMaterialBlurStyle();
                    break;
            }
        }

        private void UpdateMarginAndPadding()
        {
            if (Control == null || Element == null)
            {
                return;
            }

            Control.Margin = Element.Margin.ToPlatform();
            Control.Padding = Element.Padding.ToPlatform();
        }

        private void PackChild()
        {
            if (Element.Content == null)
            {
                return;
            }

            IVisualElementRenderer renderer = Element.Content.GetOrCreateRenderer();
            FrameworkElement frameworkElement = renderer.ContainerElement;

            _acrylicRectangle = new Rectangle();
            _shadowHost = new Rectangle { Fill = Colors.Transparent.ToBrush() };

            _grid = new Grid();
            _grid.Children.Add(_acrylicRectangle);
            _grid.Children.Add(frameworkElement);

            Control.Children.Add(_shadowHost);
            Control.Children.Add(_grid);
        }

        private void ToggleAcrylicRectangle(bool enable)
        {
            _acrylicRectangle.Margin = new Thickness(0, enable ? 2 : 0, 0, 0);
            if (!enable)
            {
                _acrylicRectangle.Fill = Colors.Transparent.ToBrush();
            }
        }

        private void UpdateBorder()
        {
            if (_grid == null)
            {
                return;
            }

            if (Element.BorderColor != null)
            {
                _grid.BorderBrush = Element.BorderColor.ToBrush();
                _grid.BorderThickness = new Thickness(1);
            }
            else
            {
                _grid.BorderBrush = new Color(0, 0, 0, 0).ToBrush();
            }
        }

        private void UpdateCornerRadius()
        {
            if (_grid == null)
            {
                return;
            }

            float cornerRadius = Element.CornerRadius;

            if (cornerRadius == -1f)
            {
                cornerRadius = 5f; // default corner radius
            }

            _grid.CornerRadius = new CornerRadius(cornerRadius);

            _shadowHost.RadiusX = cornerRadius;
            _shadowHost.RadiusY = cornerRadius;

            _acrylicRectangle.RadiusX = cornerRadius;
            _acrylicRectangle.RadiusY = cornerRadius;
        }

        private void UpdateLightThemeBackgroundColor()
        {
            if (_grid == null)
            {
                return;
            }

            switch (Element.MaterialTheme)
            {
                case MaterialFrame.Theme.Acrylic:
                    _acrylicRectangle.Fill = Element.LightThemeBackgroundColor.ToBrush();
                    break;

                case MaterialFrame.Theme.AcrylicBlur:
                case MaterialFrame.Theme.Dark:
                    return;

                case MaterialFrame.Theme.Light:
                    _grid.Background = Element.LightThemeBackgroundColor.ToBrush();
                    break;
            }
        }

        private void UpdateAcrylicGlowColor()
        {
            if (_grid == null || Element.MaterialTheme != MaterialFrame.Theme.Acrylic)
            {
                return;
            }

            _grid.Background = Element.AcrylicGlowColor.ToBrush();
        }

        private void UpdateElevation()
        {
            if (_grid == null)
            {
                return;
            }

            if (!Element.IsShadowCompatible)
            {
                _shadowHost.Fill = Colors.Transparent.ToBrush();

                if (_shadowVisual != null)
                {
                    ElementCompositionPreview.SetElementChildVisual(_shadowHost, null);
                    _shadowVisual.Dispose();
                    _shadowVisual = null;
                }

                if (Element.MaterialTheme == MaterialFrame.Theme.Dark)
                {
                    _grid.Background = Element.ElevationToColor().ToBrush();
                }

                return;
            }

            if (Element.Width < 1 || Element.Height < 1)
            {
                return;
            }

            // https://docs.microsoft.com/en-US/windows/uwp/composition/using-the-visual-layer-with-xaml
            bool isAcrylicTheme = Element.MaterialTheme == MaterialFrame.Theme.Acrylic;

            float blurRadius = isAcrylicTheme ? MaterialFrame.AcrylicElevation : Element.Elevation;
            int elevation = isAcrylicTheme ? MaterialFrame.AcrylicElevation / 3 : Element.Elevation / 2;
            float opacity = isAcrylicTheme ? 0.12f : 0.16f;

            int width = (int)Element.Width;
            int height = (int)Element.Height;

            _shadowHost.Fill = Colors.White.ToBrush();
            _shadowHost.Width = width;
            _shadowHost.Height = height;

            if (_compositor == null)
            {
                Visual hostVisual = ElementCompositionPreview.GetElementVisual(_grid);
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
            if (_grid == null)
            {
                return;
            }

            switch (Element.MaterialTheme)
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
            if (_grid == null || Element.MaterialTheme != MaterialFrame.Theme.AcrylicBlur)
            {
                return;
            }

            var acrylicBrush = new AcrylicBrush();

            // Background acrylic isn't currently supported. The mentioned property was removed in .8 release: https://github.com/microsoft/microsoft-ui-xaml/issues/6618
            // { BackgroundSource = Element.WinUIHostBackdropBlur ? AcrylicBackgroundSource.HostBackdrop : AcrylicBackgroundSource.Backdrop };

            switch (Element.MaterialBlurStyle)
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

            _grid.Background = acrylicBrush;
        }

        private void UpdateBlur()
        {
            if (_grid == null)
            {
                return;
            }

            if (Element.WinUIBlurOverlayColor != Colors.Transparent)
            {
                var acrylicBrush = new AcrylicBrush
                {
                    // Background acrylic isn't currently supported.
                    // BackgroundSource = Element.WinUIHostBackdropBlur ? AcrylicBackgroundSource.HostBackdrop : AcrylicBackgroundSource.Backdrop,
                    TintColor = Element.WinUIBlurOverlayColor.ToWindowsColor(),
                };

                _grid.Background = acrylicBrush;
                return;
            }

            UpdateMaterialBlurStyle();
        }

        private static void WarnNotImplemented(string propertyName)
        {
            InternalLogger.Warn($"The {propertyName} property is not yet available on UWP platform");
        }
    }

    internal static class ColorExtensions
    {
        public static Brush ToBrush(this Color color)
        {
            return new SolidColorBrush(color.ToWindowsColor());
        }
    }
}