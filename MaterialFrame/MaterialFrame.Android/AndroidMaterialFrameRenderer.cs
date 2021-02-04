#if __ANDROID_29__
using AndroidX.Core.View;
#else
using Android.Support.V4.View;
#endif
using System;
using System.ComponentModel;

using Android.Content;
using Android.Graphics.Drawables;
using Android.Runtime;

using Sharpnado.MaterialFrame;
using Sharpnado.MaterialFrame.Droid;

using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

using FrameRenderer = Xamarin.Forms.Platform.Android.AppCompat.FrameRenderer;
using Java.IO;

[assembly: ExportRenderer(typeof(MaterialFrame), typeof(AndroidMaterialFrameRenderer))]

namespace Sharpnado.MaterialFrame.Droid
{
    /// <summary>
    /// Renderer to update all frames with better shadows matching material design standards.
    /// </summary>
    [Preserve]
    public partial class AndroidMaterialFrameRenderer : FrameRenderer
    {
        private GradientDrawable _mainDrawable;

        private GradientDrawable _acrylicLayer;

        public AndroidMaterialFrameRenderer(Context context)
            : base(context)
        {
        }

        private MaterialFrame MaterialFrame => (MaterialFrame)Element;

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
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

                case nameof(MaterialFrame.AndroidBlurOverlayColor):
                    UpdateAndroidBlurOverlayColor();
                    break;

                case nameof(MaterialFrame.AndroidBlurRadius):
                    UpdateAndroidBlurRadius();
                    break;

                case nameof(MaterialFrame.AndroidBlurRootElement):
                    UpdateAndroidBlurRootElement();
                    break;

                case nameof(MaterialFrame.MaterialTheme):
                    UpdateMaterialTheme();
                    break;

                case nameof(MaterialFrame.MaterialBlurStyle):
                    UpdateMaterialBlurStyle();
                    break;

                default:
                    base.OnElementPropertyChanged(sender, e);
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            MaterialFrame?.Unsubscribe();
            Destroy();

            base.Dispose(disposing);
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Frame> e)
        {
            base.OnElementChanged(e);

            ((MaterialFrame)e.OldElement)?.Unsubscribe();
            Destroy();

            if (e.NewElement == null)
            {
                return;
            }

            _mainDrawable = (GradientDrawable)Background;

            UpdateMaterialTheme();
        }

        private void Destroy()
        {
            InternalLogger.Debug("AndroidMaterialFrameRenderer", "Destroy()");

            _mainDrawable = null;

            DestroyBlur();

            if (!_acrylicLayer.IsNullOrDisposed())
            {
                _acrylicLayer.Dispose();
                _acrylicLayer = null;
            }
        }

        private void UpdateCornerRadius()
        {
            _mainDrawable?.SetCornerRadius(Context.ToPixels(MaterialFrame.CornerRadius));
            _acrylicLayer?.SetCornerRadius(Context.ToPixels(MaterialFrame.CornerRadius));
            _realtimeBlurView?.SetCornerRadius(Context.ToPixels(MaterialFrame.CornerRadius));
        }

        private void UpdateElevation()
        {
            if (MaterialFrame.MaterialTheme == MaterialFrame.Theme.Dark || MaterialFrame.MaterialTheme == MaterialFrame.Theme.AcrylicBlur)
            {
                ViewCompat.SetElevation(this, 0);
                return;
            }

            bool isAcrylicTheme = MaterialFrame.MaterialTheme == MaterialFrame.Theme.Acrylic;

            // we need to reset the StateListAnimator to override the setting of Elevation on touch down and release.
            StateListAnimator = new Android.Animation.StateListAnimator();

            // set the elevation manually
            ViewCompat.SetElevation(this, isAcrylicTheme ? MaterialFrame.AcrylicElevation : MaterialFrame.Elevation);
        }

        private void UpdateLightThemeBackgroundColor()
        {
            if (MaterialFrame.MaterialTheme == MaterialFrame.Theme.Dark || MaterialFrame.MaterialTheme == MaterialFrame.Theme.AcrylicBlur)
            {
                return;
            }

            _mainDrawable.SetColor(MaterialFrame.LightThemeBackgroundColor.ToAndroid());
        }

        private void UpdateAcrylicGlowColor()
        {
            _acrylicLayer?.SetColor(MaterialFrame.AcrylicGlowColor.ToAndroid());
        }

        private void UpdateMaterialTheme()
        {
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

            Invalidate();
        }

        private void SetAcrylicBlurTheme()
        {
            _mainDrawable = new GradientDrawable();
            _mainDrawable.SetColor(Color.Transparent.ToAndroid());

            this.SetBackground(_mainDrawable);

            UpdateCornerRadius();
            UpdateElevation();

            EnableBlur();
        }

        private void SetDarkTheme()
        {
            DisableBlur();

            _mainDrawable = new GradientDrawable();
            _mainDrawable.SetColor(MaterialFrame.ElevationToColor().ToAndroid());

            this.SetBackground(_mainDrawable);

            UpdateCornerRadius();
            UpdateElevation();
        }

        private void SetLightTheme()
        {
            DisableBlur();

            _mainDrawable = new GradientDrawable();
            _mainDrawable.SetColor(MaterialFrame.LightThemeBackgroundColor.ToAndroid());

            this.SetBackground(_mainDrawable);

            UpdateCornerRadius();
            UpdateElevation();
        }

        private void SetAcrylicTheme()
        {
            if (_acrylicLayer == null)
            {
                _acrylicLayer = new GradientDrawable();
                _acrylicLayer.SetShape(ShapeType.Rectangle);
            }

            UpdateAcrylicGlowColor();
            UpdateCornerRadius();

            _mainDrawable.SetColor(MaterialFrame.LightThemeBackgroundColor.ToAndroid());

            LayerDrawable layer = new LayerDrawable(new Drawable[] { _acrylicLayer, _mainDrawable });
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
            {
                layer.SetLayerInsetTop(1, (int)Context.ToPixels(2));
            }
            else
            {
                System.Console.WriteLine(
                    $"{DateTime.Now:MM-dd H:mm:ss.fff} | Sharpnado.MaterialFrame | WARNING | The Acrylic glow is only supported on android API 23 or greater (starting from Marshmallow)");
            }

            this.SetBackground(layer);

            UpdateElevation();
        }
    }
}