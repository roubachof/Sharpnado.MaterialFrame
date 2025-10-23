using Android.Runtime;
using Android.Views;
using View = Android.Views.View;

namespace Sharpnado.MaterialFrame.Droid;

public partial class RealtimeBlurView
{
    private class PreDrawListener : Java.Lang.Object, ViewTreeObserver.IOnPreDrawListener
    {
        private readonly JniWeakReference<RealtimeBlurView> _weakBlurView;
        private long _lastProcessedTime = 0;

        public PreDrawListener(RealtimeBlurView blurView)
        {
            _weakBlurView = new JniWeakReference<RealtimeBlurView>(blurView);
        }

        public PreDrawListener(IntPtr handle, JniHandleOwnership transfer)
            : base(handle, transfer)
        {
        }

        public bool OnPreDraw()
        {
            if (!_weakBlurView.TryGetTarget(out var blurView))
            {
                return false;
            }

            if (!blurView._isContainerShown)
            {
                return false;
            }

            // Throttle to prevent infinite loops from alpha changes and reduce CPU usage
            long currentTime = Java.Lang.JavaSystem.CurrentTimeMillis();
            if (currentTime - _lastProcessedTime < AndroidMaterialFrameHandler.BlurUpdateIntervalMs)
            {
                return true;
            }
            _lastProcessedTime = currentTime;

            try
            {
                var mDecorView = blurView.GetRootView();

                InternalLogger.DebugIf(LogDrawing, $"BlurView@{blurView.GetHashCode()}", () => $"OnPreDraw()");

                int[] locations = new int[2];
                Android.Graphics.Bitmap oldBmp = blurView.mBlurredBitmap;
                View decor = mDecorView;
                if (!decor.IsNullOrDisposed() && blurView.IsShown && blurView.Prepare())
                {
                    InternalLogger.DebugIf(LogDrawing, $"BlurView@{blurView.GetHashCode()}",
                        () => $"OnPreDraw(formsId: {blurView._formsId}) => calling draw on decor");
                    bool redrawBitmap = blurView.mBlurredBitmap != oldBmp;
                    oldBmp = null;
                    decor.GetLocationOnScreen(locations);
                    int x = -locations[0];
                    int y = -locations[1];

                    blurView.GetLocationOnScreen(locations);
                    x += locations[0];
                    y += locations[1];

                    // just erase transparent
                    blurView.mBitmapToBlur.EraseColor(blurView.mOverlayColor & 0xffffff);

                int rc = blurView.mBlurringCanvas.Save();
                blurView.mIsRendering = true;
                RENDERING_COUNT++;

                // Temporarily make parent transparent during capture
                // Time throttling prevents the infinite loop this would otherwise cause
                float originalParentAlpha = 1.0f;
                View parent = blurView.Parent as View;
                if (parent != null)
                {
                    originalParentAlpha = parent.Alpha;
                    parent.Alpha = 0f;
                }

                try
                {
                        blurView.mBlurringCanvas.Scale(
                            1f * blurView.mBitmapToBlur.Width / blurView.Width,
                            1f * blurView.mBitmapToBlur.Height / blurView.Height);
                        blurView.mBlurringCanvas.Translate(-x, -y);
                        if (decor.Background != null)
                        {
                            decor.Background.Draw(blurView.mBlurringCanvas);
                        }

                        decor.Draw(blurView.mBlurringCanvas);
                    }
                    catch (StopException)
                    {
                        InternalLogger.DebugIf(LogDrawing, $"BlurView@{blurView.GetHashCode()}",
                            () => $"OnPreDraw(formsId: {blurView._formsId}) => in catch StopException");
                    }
                    catch (System.Exception)
                    {
                        InternalLogger.Error($"BlurView@{blurView.GetHashCode()}",
                            () => $"OnPreDraw(formsId: {blurView._formsId}) => in catch global exception");
                    }
                finally
                {
                    // Restore original alpha
                    if (parent != null)
                    {
                        parent.Alpha = originalParentAlpha;
                    }

                    blurView.mIsRendering = false;
                    RENDERING_COUNT--;
                    blurView.mBlurringCanvas.RestoreToCount(rc);
                }

                    InternalLogger.DebugIf(LogDrawing, $"BlurView@{blurView.GetHashCode()}",
                        () => $"OnPreDraw(formsId: {blurView._formsId}) => scheduling async blur");
                    blurView.ScheduleAsyncBlur();

                    if (redrawBitmap || blurView.mDifferentRoot)
                    {
                        InternalLogger.DebugIf(
                            LogDrawing,
                            $"BlurView@{blurView.GetHashCode()}",
                            () =>
                                $"OnPreDraw(formsId: {blurView._formsId}, redrawBitmap: {redrawBitmap}, differentRoot: {blurView.mDifferentRoot}) => blurView.Invalidate()");
                        blurView.Invalidate();
                    }
            }
            }
            catch (System.Exception ex)
            {
                InternalLogger.Error($"BlurView OnPreDraw error", () => $"Exception: {ex.Message}");
            }

            return true;
        }
    }
}
