using System.ComponentModel;
using System.Numerics;

using Windows.UI.Composition;
using Windows.UI.Xaml.Media;

using Sharpnado.MaterialFrame;
using Sharpnado.MaterialFrame.UWP;

using Xamarin.Forms;
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
    public class UWPMaterialFrameRenderer : FrameRenderer
    {
        private static readonly Color DarkBlurOverlayColor = Color.FromHex("#80000000");

        private static readonly Color LightBlurOverlayColor = Color.FromHex("#40FFFFFF");

        private static readonly Color ExtraLightBlurOverlayColor = Color.FromHex("#B0FFFFFF");

        private MaterialFrame MaterialFrame => Element as MaterialFrame;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
            }
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Frame> e)
        {
            base.OnElementChanged(e);

            ((MaterialFrame)e.OldElement)?.Unsubscribe();

            if (e.NewElement == null)
            {
                return;
            }

            UpdateMaterialTheme();
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MaterialFrame.BorderColor):
                case nameof(MaterialFrame.CornerRadius):
                    base.OnElementPropertyChanged(sender, e);
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

        private void UpdateLightThemeBackgroundColor()
        {
            switch (MaterialFrame.MaterialTheme)
            {
                case MaterialFrame.Theme.Acrylic:
                    Control.Background = MaterialFrame.LightThemeBackgroundColor.ToBrush();
                    break;

                case MaterialFrame.Theme.AcrylicBlur:
                case MaterialFrame.Theme.Dark:
                    return;

                case MaterialFrame.Theme.Light:
                    Control.Background = MaterialFrame.LightThemeBackgroundColor.ToBrush();
                    break;
            }
        }

        private void UpdateAcrylicGlowColor()
        {
            if (MaterialFrame.MaterialTheme != MaterialFrame.Theme.Acrylic)
            {
                return;
            }

            WarnNotImplemented(nameof(MaterialFrame.AcrylicGlowColor));
        }

        private void UpdateElevation()
        {
            if (MaterialFrame.MaterialTheme == MaterialFrame.Theme.AcrylicBlur)
            {
                return;
            }

            if (MaterialFrame.MaterialTheme == MaterialFrame.Theme.Dark)
            {
                Control.Shadow = null;
                Control.Background = MaterialFrame.ElevationToColor().ToBrush();
                return;
            }

            // TODO: composition with visual, needs a Rectangle and not a Border
            // https://docs.microsoft.com/en-US/windows/uwp/composition/composition-shadows
            //bool isAcrylicTheme = MaterialFrame.MaterialTheme == MaterialFrame.Theme.Acrylic;
            //int elevation = isAcrylicTheme ? MaterialFrame.AcrylicElevation : MaterialFrame.Elevation;
            //float opacity = isAcrylicTheme ? 0.12f : 0.24f;

            //var compositor = new Compositor();
            //var basicShadow = compositor.CreateDropShadow();
            //basicShadow.BlurRadius = 25f;
            //basicShadow.Opacity = opacity;
            //basicShadow.Color = Color.Black.ToWindowsColor();
            //basicShadow.Offset = new Vector3(0, 20, elevation);

            //Control.Shadow = basicShadow;

            InternalLogger.Warn($"The {nameof(MaterialFrame.Elevation)} property is only implemented for dark mode on UWP");
        }

        private void UpdateCornerRadius()
        {
        }

        private void UpdateMaterialTheme()
        {
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
            UpdateCornerRadius();
            UpdateElevation();
        }

        private void SetLightTheme()
        {
            UpdateLightThemeBackgroundColor();
            UpdateCornerRadius();
            UpdateElevation();
        }

        private void SetAcrylicTheme()
        {
            UpdateLightThemeBackgroundColor();
            UpdateAcrylicGlowColor();

            UpdateCornerRadius();
            UpdateElevation();
        }

        private void SetAcrylicBlurTheme()
        {
            UpdateBlur();
            UpdateCornerRadius();
            UpdateElevation();
        }

        private void UpdateMaterialBlurStyle()
        {
            var acrylicBrush = new AcrylicBrush { BackgroundSource = AcrylicBackgroundSource.Backdrop };

            switch (MaterialFrame.MaterialBlurStyle)
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
            if (MaterialFrame.UwpBlurOverlayColor != Color.Default)
            {
                var acrylicBrush = new AcrylicBrush
                    {
                        BackgroundSource = AcrylicBackgroundSource.Backdrop,
                        TintColor = MaterialFrame.UwpBlurOverlayColor.ToWindowsColor(),
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