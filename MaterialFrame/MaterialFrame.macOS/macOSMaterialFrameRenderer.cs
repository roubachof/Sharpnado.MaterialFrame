using AppKit;

using System;
using System.ComponentModel;

using CoreAnimation;

using CoreGraphics;

using Foundation;

using Sharpnado.MaterialFrame;
using Sharpnado.MaterialFrame.macOS;

using Xamarin.Forms;
using Xamarin.Forms.Platform.MacOS;

using Color = Xamarin.Forms.Color;

[assembly: ExportRenderer(typeof(MaterialFrame), typeof(macOSMaterialFrameRenderer))]

namespace Sharpnado.MaterialFrame.macOS
{
    /// <summary>
    ///     Renderer to update all frames with better shadows matching material design standards.
    /// </summary>
    [Preserve]
    public class macOSMaterialFrameRenderer : VisualElementRenderer<MaterialFrame>, ICALayerDelegate
    {
        private const string Tag = nameof(macOSMaterialFrameRenderer);

        private CALayer _intermediateLayer;

        private NSVisualEffectView _blurView;

        public static new void Init()
        {
            var preserveRenderer = typeof(macOSMaterialFrameRenderer);
            var preserveMaterialFrame = typeof(MaterialFrame);
        }

        [Export("layoutSublayersOfLayer:")]
        public void LayoutSublayersOfLayer(CALayer layer)
        {
            if (Layer.Bounds.Width > 0 && Layer.ShadowRadius > 0)
            {
                float radius = Element.CornerRadius;
                if (radius == -1.0f)
                {
                    radius = 5f;
                }

                Layer.ShadowPath = CGPath.FromRoundedRect(Layer.Bounds, radius, radius);
            }
        }

        public override bool AllowsVibrancy => true;

        public override void Layout()
        {
            base.Layout();
            InternalLogger.Debug(Tag, () => $"Layout()");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            InternalLogger.Debug(Tag, () => $"Dispose( disposing: {disposing} )");

            if (disposing)
            {
                _intermediateLayer?.RemoveFromSuperLayer();
                _intermediateLayer?.Dispose();
                _intermediateLayer = null;

                _blurView?.RemoveFromSuperview();
                _blurView?.Dispose();
                _blurView = null;
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

            InternalLogger.Debug(Tag, () => "OnElementChanged()");

            _intermediateLayer = new CALayer { BackgroundColor = Color.Transparent.ToCGColor() };
            WantsLayer = true;
            Layer.InsertSublayer(_intermediateLayer, 0);
            Layer.Delegate = this;
            UpdateMaterialTheme();
        }

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

                case nameof(MaterialFrame.MaterialTheme):
                    UpdateMaterialTheme();
                    break;

                case nameof(MaterialFrame.MaterialBlurStyle):
                    UpdateMaterialBlurStyle();
                    break;

                case nameof(MaterialFrame.Height):
                case nameof(MaterialFrame.Width):
                    UpdateBlurViewBounds();
                    UpdateLayerBounds();
                    break;
            }
        }

        private static bool SizeAreEqual(CGRect frame, MaterialFrame element)
        {
            return frame.Width == element.Width && frame.Height == element.Height;
        }

        private void UpdateLightThemeBackgroundColor()
        {
            InternalLogger.Debug(Tag, () => $"UpdateLightThemeBackgroundColor() => LightThemeBackgroundColor: {Element.LightThemeBackgroundColor}");
            switch (Element.MaterialTheme)
            {
                case MaterialFrame.Theme.Acrylic:
                    _intermediateLayer.BackgroundColor = Element.LightThemeBackgroundColor.ToCGColor();
                    break;

                case MaterialFrame.Theme.AcrylicBlur:
                case MaterialFrame.Theme.Dark:
                    return;

                case MaterialFrame.Theme.Light:
                    Layer.BackgroundColor = Element.LightThemeBackgroundColor.ToCGColor();
                    break;
            }
        }

        private void UpdateAcrylicGlowColor()
        {
            if (Element.MaterialTheme != MaterialFrame.Theme.Acrylic)
            {
                return;
            }

            InternalLogger.Debug(Tag, () => "UpdateAcrylicGlowColor()");
            Layer.BackgroundColor = Element.AcrylicGlowColor.ToCGColor();
        }

        private void UpdateElevation()
        {
            InternalLogger.Debug(Tag, () => "UpdateElevation()");

            if (Element.MaterialTheme == MaterialFrame.Theme.AcrylicBlur)
            {
                Layer.ShadowOpacity = 0.0f;

                Layer.ShouldRasterize = false;
                return;
            }

            if (Element.MaterialTheme == MaterialFrame.Theme.Dark)
            {
                Layer.ShadowOpacity = 0.0f;
                Layer.BackgroundColor = Element.ElevationToColor().ToCGColor();
                return;
            }

            bool isAcrylicTheme = Element.MaterialTheme == MaterialFrame.Theme.Acrylic;

            float adaptedElevation = isAcrylicTheme ? MaterialFrame.AcrylicElevation / 3 : Element.Elevation / 2;
            float opacity = isAcrylicTheme ? 0.12f : 0.24f;

            Layer.ShadowColor = NSColor.Black.CGColor;
            Layer.ShadowRadius = Math.Abs(adaptedElevation);
            Layer.ShadowOffset = new CGSize(0, -adaptedElevation);
            Layer.ShadowOpacity = opacity;

            Layer.MasksToBounds = false;

            Layer.RasterizationScale = NSScreen.MainScreen.BackingScaleFactor;
            Layer.ShouldRasterize = true;
        }

        private void UpdateCornerRadius()
        {
            InternalLogger.Debug(Tag, () => "UpdateCornerRadius()");

            float radius = Element.CornerRadius;
            if (radius == -1.0f)
            {
                radius = 5f;
            }

            Layer.CornerRadius = radius;
            _intermediateLayer.CornerRadius = radius;

            if (_blurView != null)
            {
                _blurView.Layer.CornerRadius = radius;
            }
        }

        private void UpdateMaterialTheme()
        {
            InternalLogger.Debug(Tag, () => $"UpdateMaterialTheme() => MaterialTheme: {Element.MaterialTheme}");
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
        }

        private void SetDarkTheme()
        {
            InternalLogger.Debug(Tag, () => "SetDarkTheme()");

            _intermediateLayer.BackgroundColor = Color.Transparent.ToCGColor();

            Layer.BackgroundColor = Element.ElevationToColor().ToCGColor();

            UpdateCornerRadius();
            UpdateElevation();

            DisableBlur();
        }

        private void SetLightTheme()
        {
            InternalLogger.Debug(Tag, () => "SetLightTheme()");

            _intermediateLayer.BackgroundColor = Color.Transparent.ToCGColor();

            Layer.BackgroundColor = Element.LightThemeBackgroundColor.ToCGColor();

            UpdateCornerRadius();
            UpdateElevation();

            DisableBlur();
        }

        private void SetAcrylicTheme()
        {
            InternalLogger.Debug(Tag, () => "SetAcrylicTheme()");

            _intermediateLayer.BackgroundColor = Element.LightThemeBackgroundColor.ToCGColor();

            UpdateAcrylicGlowColor();

            UpdateCornerRadius();
            UpdateElevation();

            DisableBlur();
        }

        private void SetAcrylicBlurTheme()
        {
            InternalLogger.Debug(Tag, () => "SetAcrylicBlurTheme()");

            _intermediateLayer.BackgroundColor = Color.Transparent.ToCGColor();
            Layer.BackgroundColor = Color.Transparent.ToCGColor();
            EnableBlur();

            UpdateCornerRadius();
            UpdateElevation();
        }

        private void UpdateMaterialBlurStyle()
        {
            InternalLogger.Debug(Tag, () => "UpdateMaterialBlurStyle()");

            if (_blurView != null)
            {
                _blurView.BlendingMode = Element.MacOSBehindWindowBlur
                                             ? NSVisualEffectBlendingMode.BehindWindow
                                             : NSVisualEffectBlendingMode.WithinWindow;
                _blurView.Material = ConvertBlurStyle();
            }
        }

        private void EnableBlur()
        {
            InternalLogger.Debug(Tag, () => "EnableBlur()");
            if (_blurView == null)
            {
                _blurView = new macOSVisualEffectView() { WantsLayer = true };
                _blurView.Layer.MasksToBounds = true;
            }

            UpdateMaterialBlurStyle();

            if (Subviews.Length > 0 && ReferenceEquals(Subviews[0], _blurView))
            {
                return;
            }

            UpdateBlurViewBounds();
            AddSubview(_blurView, NSWindowOrderingMode.Below, this);
        }

        private void DisableBlur()
        {
            InternalLogger.Debug(Tag, () => "DisableBlur()");

            _blurView?.RemoveFromSuperview();
        }

        private void UpdateLayerBounds()
        {
            if (Element.Width > 0 && Element.Height > 0 && !SizeAreEqual(_intermediateLayer.Frame, Element))
            {
                InternalLogger.Debug(Tag, () => "UpdateLayerBounds()");

                _intermediateLayer.Frame = new CGRect(0, 0, Element.Width, Element.Height - 2);
                _intermediateLayer.RemoveAllAnimations();
            }
        }

        private void UpdateBlurViewBounds()
        {
            if (_blurView != null && Element.Width > 0 && Element.Height > 0 && !SizeAreEqual(_blurView.Frame, Element))
            {
                InternalLogger.Debug(Tag, () => "UpdateBlurViewBounds()");

                _blurView.Frame = new CGRect(0, 0, Element.Width, Element.Height);
            }
        }

        private NSVisualEffectMaterial ConvertBlurStyle()
        {
            switch (Element.MaterialBlurStyle)
            {
                case MaterialFrame.BlurStyle.ExtraLight:
                    return NSVisualEffectMaterial.Light;
                case MaterialFrame.BlurStyle.Dark:
                    return NSVisualEffectMaterial.Dark;

                default:
                    return NSVisualEffectMaterial.Light;
            }
        }
    }

    internal class macOSVisualEffectView : NSVisualEffectView
    {
        public override bool AllowsVibrancy => true;

        public override bool WantsDefaultClipping => false;
    }
}