using Android.Views;
using Android.Widget;
using Microsoft.Maui.Platform;
using View = Android.Views.View;

namespace Sharpnado.MaterialFrame.Droid;

public partial class AndroidMaterialFrameHandler
{
    private const double StyledBlurRadius = 64;

    private static readonly Color DarkBlurOverlayColor = Color.FromArgb("#80000000");

    private static readonly Color LightBlurOverlayColor = Color.FromArgb("#40FFFFFF");

    private static readonly Color ExtraLightBlurOverlayColor = Color.FromArgb("#B0FFFFFF");

    private static int _blurAutoUpdateDelayMilliseconds = 20;
    private static int _blurProcessingDelayMilliseconds = 10;

    private RealtimeBlurView? _realtimeBlurView;

    private View? _blurRootView;

    /// <summary>
    /// When a page visibility changes we activate or deactivate blur updates.
    /// Setting a bigger delay could improve performance and rendering.
    /// </summary>
    public static int BlurAutoUpdateDelayMilliseconds
    {
        get => _blurAutoUpdateDelayMilliseconds;
        set
        {
            if (value < 0)
            {
                throw new ArgumentException(
                    "The blur processing delay cannot be negative",
                    nameof(BlurAutoUpdateDelayMilliseconds));
            }

            _blurAutoUpdateDelayMilliseconds = value;
        }
    }

    /// <summary>
    /// Sometimes the computation of the background can take some times (svg images for example).
    /// Setting a bigger delay to be sure that the background is rendered first can fix some glitches.
    /// </summary>
    public static int BlurProcessingDelayMilliseconds
    {
        get => _blurProcessingDelayMilliseconds;
        set
        {
            if (value < 0)
            {
                throw new ArgumentException(
                    "The blur processing delay cannot be negative",
                    nameof(BlurProcessingDelayMilliseconds));
            }

            _blurProcessingDelayMilliseconds = value;
        }
    }

    /// <summary>
    /// If set to <see langword="true"/>, the rendering result could be better (clearer blur not mixing front elements).
    /// However due to a bug in the Xamarin framework https://github.com/xamarin/xamarin-android/issues/4548, debugging is impossible with this mode (causes SIGSEGV).
    /// A suggestion would be to set it to false for debug, and to true for releases.
    /// </summary>
    public static bool ThrowStopExceptionOnDraw { get; set; } = false;

    private bool IsAndroidBlurPropertySet => MaterialFrame.AndroidBlurRadius > 0;

    private double CurrentBlurRadius =>
        IsAndroidBlurPropertySet ? MaterialFrame.AndroidBlurRadius : StyledBlurRadius;

    private string FormsId => MaterialFrame.StyleId;

    public override void PlatformArrange(Rect rect)
    {
        base.PlatformArrange(rect);

        InternalLogger.Debug(FormsId, () => $"Renderer::OnSizeChanged(w: {rect.Width}, h: {rect.Height})");
        LayoutBlurView((int)rect.Width, (int)rect.Height);
    }

    private void LayoutBlurView(int width, int height)
    {
        if (width == 0 || height == 0 || _realtimeBlurView == null)
        {
            return;
        }

        InternalLogger.Debug(FormsId, () => $"Renderer::LayoutBlurView(element: {MaterialFrame.StyleId}, size: {width}w, {height}h)");

        int androidWidth = (int)Context.ToPixels(width);
        int androidHeight = (int)Context.ToPixels(height);

        _realtimeBlurView.Measure(androidWidth, androidHeight);
        _realtimeBlurView.Layout(0, 0, androidWidth, androidHeight);
    }

    private void DestroyBlur()
    {
        if (_realtimeBlurView.IsNullOrDisposed())
        {
            return;
        }

        InternalLogger.Debug(FormsId, () => "Renderer::DestroyBlur()");

        _realtimeBlurView!.Destroy();
        _realtimeBlurView = null;
    }

    private void UpdateAndroidBlurRootElement()
    {
        if (MaterialFrame.AndroidBlurRootElement == null)
        {
            return;
        }

        var formsView = MaterialFrame.AndroidBlurRootElement;

        bool IsAncestor(Element child, Layout parent)
        {
            if (child.Parent == null)
            {
                return false;
            }

            if (child.Parent == parent)
            {
                return true;
            }

            return IsAncestor(child.Parent, parent);
        }

        if (!IsAncestor(MaterialFrame, MaterialFrame.AndroidBlurRootElement))
        {
            throw new InvalidOperationException(
                "The AndroidBlurRootElement of the MaterialFrame should be an ancestor of the MaterialFrame.");
        }

        _blurRootView = formsView.ToPlatform(MauiContext!);

        _realtimeBlurView?.SetRootView(_blurRootView);
    }

    private void UpdateAndroidBlurOverlayColor(bool invalidate = true)
    {
        if (IsAndroidBlurPropertySet)
        {
            InternalLogger.Debug(FormsId, () => "UpdateAndroidBlurOverlayColor()");
            _realtimeBlurView?.SetOverlayColor(MaterialFrame.AndroidBlurOverlayColor.ToPlatform(), invalidate);
        }
    }

    private void UpdateAndroidBlurRadius(bool invalidate = true)
    {
        if (IsAndroidBlurPropertySet)
        {
            InternalLogger.Debug(FormsId, () => "Renderer::UpdateAndroidBlurRadius()");
            _realtimeBlurView?.SetBlurRadius(Context.ToPixels(MaterialFrame.AndroidBlurRadius), invalidate);
        }
    }

    private void UpdateMaterialBlurStyle(bool invalidate = true)
    {
        if (_realtimeBlurView.IsNullOrDisposed() || IsAndroidBlurPropertySet)
        {
            return;
        }

        InternalLogger.Debug(FormsId, () => $"Renderer::UpdateMaterialBlurStyle({MaterialFrame.MaterialBlurStyle})");

        _realtimeBlurView!.SetBlurRadius(Context.ToPixels(StyledBlurRadius), invalidate);

        switch (MaterialFrame.MaterialBlurStyle)
        {
            case MaterialFrame.BlurStyle.ExtraLight:
                _realtimeBlurView.SetOverlayColor(ExtraLightBlurOverlayColor.ToPlatform(), invalidate);
                break;
            case MaterialFrame.BlurStyle.Dark:
                _realtimeBlurView.SetOverlayColor(DarkBlurOverlayColor.ToPlatform(), invalidate);
                break;

            default:
                _realtimeBlurView.SetOverlayColor(LightBlurOverlayColor.ToPlatform(), invalidate);
                break;
        }
    }

    private void EnableBlur()
    {
        InternalLogger.Debug(FormsId, () => "Renderer::EnableBlur()");

        if (_realtimeBlurView == null)
        {
            _realtimeBlurView = new RealtimeBlurView(Context, MaterialFrame.StyleId);
        }

        UpdateAndroidBlurRadius();
        UpdateAndroidBlurOverlayColor();
        UpdateMaterialBlurStyle();
        UpdateAndroidBlurRootElement();

        _realtimeBlurView.SetDownsampleFactor(CurrentBlurRadius <= 10 ? 1 : 2);

        UpdateCornerRadius();

        if (PlatformView.ChildCount > 0 && ReferenceEquals(PlatformView.GetChildAt(0), _realtimeBlurView))
        {
            // Already added
            return;
        }

        InternalLogger.Debug(FormsId, () => $"Renderer::EnableBlur() => adding realtimeview (w{PlatformView.Width}, h{PlatformView.Height})");
        PlatformView.AddView(
            _realtimeBlurView,
            0,
            new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent,
                GravityFlags.NoGravity));

        LayoutBlurView(PlatformView.Width, PlatformView.Height);
    }

    private void DisableBlur()
    {
        if (PlatformView.ChildCount == 0 || !ReferenceEquals(PlatformView.GetChildAt(0), _realtimeBlurView))
        {
            return;
        }

        InternalLogger.Debug(FormsId, () => "Renderer::DisableBlur() => removing pre draw listener");
        PlatformView.RemoveView(_realtimeBlurView);
    }
}
