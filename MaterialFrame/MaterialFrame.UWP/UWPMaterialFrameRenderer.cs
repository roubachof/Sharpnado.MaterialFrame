using System.ComponentModel;
using System.Numerics;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;

using Sharpnado.MaterialFrame;
using Sharpnado.MaterialFrame.UWP;

using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.UWP;

using AcrylicBackgroundSource = Microsoft.UI.Xaml.Media.AcrylicBackgroundSource;
using AcrylicBrush = Microsoft.UI.Xaml.Media.AcrylicBrush;
using Color = Xamarin.Forms.Color;

[assembly: ExportRenderer(typeof(MaterialFrame), typeof(UWPMaterialFrameRenderer))]

namespace Sharpnado.MaterialFrame.UWP
{
    /// <summary>
    ///     Renderer to update all frames with better shadows matching material design standards.
    /// </summary>
    [Preserve]
    public class UWPMaterialFrameRenderer : ViewRenderer<MaterialFrame, Windows.UI.Xaml.Controls.Grid>
    {
        private static readonly Color DarkBlurOverlayColor = Color.FromHex("#80000000");

        private static readonly Color LightBlurOverlayColor = Color.FromHex("#40FFFFFF");

        private static readonly Color ExtraLightBlurOverlayColor = Color.FromHex("#B0FFFFFF");

        private Rectangle _rectangle;

        public UWPMaterialFrameRenderer()
        {
            AutoPackage = false;
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

            if (Control == null)
            {
                SetNativeControl(new Grid());
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
                case nameof(MaterialFrame.BorderColor):
                    UpdateBorder();
                    break;

                case nameof(MaterialFrame.CornerRadius):
                    UpdateCornerRadius();
                    break;

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

                case nameof(MaterialFrame.UwpBlurOverlayColor):
                    UpdateBlur();
                    break;

                case nameof(MaterialFrame.MaterialBlurStyle):
                    UpdateMaterialBlurStyle();
                    break;
            }
        }

        private void PackChild()
        {
            if (Element.Content == null)
            {
                return;
            }

            IVisualElementRenderer renderer = Element.Content.GetOrCreateRenderer();
            FrameworkElement frameworkElement = renderer.ContainerElement;

            _rectangle = new Rectangle();

            Control.Children.Add(_rectangle);
            Control.Children.Add(frameworkElement);
        }

        private void ToggleAcrylicRectangle(bool enable)
        {
            _rectangle.Margin = new Thickness(0, enable ? 2 : 0, 0, 0);
            if (!enable)
            {
                _rectangle.Fill = Color.Transparent.ToBrush();
            }
        }

        private void UpdateBorder()
        {
            if (Element.BorderColor != Color.Default)
            {
                Control.BorderBrush = Element.BorderColor.ToBrush();
                Control.BorderThickness = new Thickness(1);
            }
            else
            {
                Control.BorderBrush = new Color(0, 0, 0, 0).ToBrush();
            }
        }

        private void UpdateCornerRadius()
        {
            float cornerRadius = Element.CornerRadius;

            if (cornerRadius == -1f)
            {
                cornerRadius = 5f; // default corner radius
            }

            Control.CornerRadius = new CornerRadius(cornerRadius);
            _rectangle.RadiusX = cornerRadius;
            _rectangle.RadiusY = cornerRadius;
        }

        private void UpdateLightThemeBackgroundColor()
        {
            switch (Element.MaterialTheme)
            {
                case MaterialFrame.Theme.Acrylic:
                    _rectangle.Fill = Element.LightThemeBackgroundColor.ToBrush();
                    break;

                case MaterialFrame.Theme.AcrylicBlur:
                case MaterialFrame.Theme.Dark:
                    return;

                case MaterialFrame.Theme.Light:
                    Control.Background = Element.LightThemeBackgroundColor.ToBrush();
                    break;
            }
        }

        private void UpdateAcrylicGlowColor()
        {
            if (Element.MaterialTheme != MaterialFrame.Theme.Acrylic)
            {
                return;
            }

            Control.Background = Element.AcrylicGlowColor.ToBrush();
        }

        private void UpdateElevation()
        {
            if (!Element.IsShadowCompatible)
            {
                // Translation = new Vector3(0, 0, 0);

                if (Element.MaterialTheme == MaterialFrame.Theme.Dark)
                {
                    Control.Background = Element.ElevationToColor().ToBrush();
                }

                return;
            }

            // Can't do this since the shadow cannot cross the grid layout boundaries
            // https://docs.microsoft.com/en-US/windows/uwp/composition/using-the-visual-layer-with-xaml
            // https://github.com/xamarin/Xamarin.Forms/issues/7839
            //bool isAcrylicTheme = Element.MaterialTheme == MaterialFrame.Theme.Acrylic;
            //int elevation = isAcrylicTheme ? MaterialFrame.AcrylicElevation : Element.Elevation;
            //float opacity = isAcrylicTheme ? 0.12f : 0.24f;

            // Translation = new Vector3(0, 0, elevation);

            //var dropShadow = _compositor.CreateDropShadow();
            //dropShadow.BlurRadius = 25f;
            //dropShadow.Opacity = opacity;
            //dropShadow.Color = Color.Black.ToWindowsColor();
            //dropShadow.Offset = new Vector3(0, elevation, elevation);

            InternalLogger.Warn($"The {nameof(MaterialFrame.Elevation)} property is only implemented for dark mode on UWP");
        }

        private void UpdateMaterialTheme()
        {
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
            var acrylicBrush = new AcrylicBrush { BackgroundSource = AcrylicBackgroundSource.Backdrop };

            switch (Element.MaterialBlurStyle)
            {
                case MaterialFrame.BlurStyle.ExtraLight:
                    acrylicBrush.TintColor = ExtraLightBlurOverlayColor.ToWindowsColor();
                    break;
                case MaterialFrame.BlurStyle.Dark:
                    acrylicBrush.TintColor = DarkBlurOverlayColor.ToWindowsColor();
                    break;

                default:
                    acrylicBrush.TintColor = LightBlurOverlayColor.ToWindowsColor();
                    break;
            }

            Control.Background = acrylicBrush;
        }

        private void UpdateBlur()
        {
            if (Element.UwpBlurOverlayColor != Color.Default)
            {
                var acrylicBrush = new AcrylicBrush
                    {
                        BackgroundSource = AcrylicBackgroundSource.Backdrop,
                        TintColor = Element.UwpBlurOverlayColor.ToWindowsColor(),
                    };

                Control.Background = acrylicBrush;
                return;
            }

            UpdateMaterialBlurStyle();
        }

        private static void WarnNotImplemented(string propertyName)
        {
            InternalLogger.Warn($"The {propertyName} property is not yet available on UWP platform");
        }
    }
}