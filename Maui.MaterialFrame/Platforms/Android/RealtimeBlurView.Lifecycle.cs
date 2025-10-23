using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using View = Android.Views.View;

namespace Sharpnado.MaterialFrame.Droid;

public partial class RealtimeBlurView
{
    protected override void OnAttachedToWindow()
    {
        InternalLogger.Debug($"BlurView@{GetHashCode()}", () => $"OnAttachedToWindow()");
        base.OnAttachedToWindow();

        var mDecorView = GetRootView();
        if (mDecorView == null)
        {
            SetRootView(GetActivityDecorView());
        }
        else
        {
            OnAttached(mDecorView);
        }
    }

    protected override void OnDetachedFromWindow()
    {
        var mDecorView = GetRootView();
        if (mDecorView != null)
        {
            UnsubscribeToPreDraw(mDecorView);
        }

        _isDetached = true;
        InternalLogger.Debug($"BlurView@{GetHashCode()}", () => $"OnDetachedFromWindow()");

        // Don't release the root view - we might reattach (e.g., navigation back)
        // Only release bitmaps and cancel blur operations
        // ReleaseBitmap();

        try
        {
            _blurCts?.Cancel();
        }
        catch { }
        finally
        {
            _blurCts?.Dispose();
            _blurCts = new CancellationTokenSource();
        }

        base.OnDetachedFromWindow();
    }

    protected override void OnVisibilityChanged(View changedView, [GeneratedEnum] ViewStates visibility)
    {
        base.OnVisibilityChanged(changedView, visibility);

        if (changedView.GetType().Name == "PageContainer")
        {
            _isContainerShown = visibility == ViewStates.Visible;
            SetAutoUpdate(_isContainerShown);
        }
    }

    private void OnAttached(View mDecorView)
    {
        if (mDecorView != null)
        {
            using var handler = new Handler();
            bool isReattached = _isDetached;
            handler.PostDelayed(
                () =>
                {
                    SubscribeToPreDraw(mDecorView);
                    mDifferentRoot = mDecorView.RootView != RootView;
                    if (mDifferentRoot || isReattached)
                    {
                        mDecorView.PostInvalidate();
                    }
                },
                AndroidMaterialFrameHandler.BlurProcessingDelayMilliseconds);
        }
        else
        {
            mDifferentRoot = false;
        }

        _isDetached = false;
    }

    protected View GetActivityDecorView()
    {
        Context ctx = Context;
        for (int i = 0; i < 4 && ctx != null && !(ctx is Activity) && ctx is ContextWrapper; i++)
        {
            ctx = ((ContextWrapper)ctx).BaseContext;
        }

        if (ctx is Activity)
        {
            return ((Activity)ctx).Window.DecorView;
        }
        else
        {
            return null;
        }
    }

    public void Destroy()
    {
        InternalLogger.Debug($"BlurView@{GetHashCode()}", () => "Destroy()");

        if (_weakDecorView != null && _weakDecorView.TryGetTarget(out var mDecorView))
        {
            UnsubscribeToPreDraw(mDecorView);
        }

        Release();
        _weakDecorView = null;
    }

    public void Release()
    {
        SetRootView(null);
        ReleaseBitmap();

        try
        {
            _blurCts?.Cancel();
        }
        catch { }
        finally
        {
            _blurCts?.Dispose();
            _blurCts = null;
        }

        mBlurImpl?.Release();
    }

    private void SubscribeToPreDraw(View decorView)
    {
        if (decorView.IsNullOrDisposed() || decorView.ViewTreeObserver.IsNullOrDisposed())
        {
            return;
        }

        decorView.ViewTreeObserver.AddOnPreDrawListener(preDrawListener);
    }

    private void UnsubscribeToPreDraw(View decorView)
    {
        if (decorView.IsNullOrDisposed() || decorView.ViewTreeObserver.IsNullOrDisposed())
        {
            return;
        }

        decorView.ViewTreeObserver.RemoveOnPreDrawListener(preDrawListener);
    }

    private void SetAutoUpdate(bool autoUpdate)
    {
        if (autoUpdate)
        {
            EnableAutoUpdate();
            return;
        }

        DisableAutoUpdate();
    }

    private void EnableAutoUpdate()
    {
        if (_autoUpdate)
        {
            return;
        }

        InternalLogger.Debug($"BlurView@{GetHashCode()}", () => $"EnableAutoUpdate()");

        _autoUpdate = true;
        using var handler = new Handler();
        handler.PostDelayed(
            () =>
            {
                var mDecorView = GetRootView();
                if (mDecorView == null || !_autoUpdate)
                {
                    return;
                }

                SubscribeToPreDraw(mDecorView);
            },
            AndroidMaterialFrameHandler.BlurAutoUpdateDelayMilliseconds);
    }

    private void DisableAutoUpdate()
    {
        if (!_autoUpdate)
        {
            return;
        }

        InternalLogger.Debug($"BlurView@{GetHashCode()}", () => $"DisableAutoUpdate()");

        _autoUpdate = false;
        var mDecorView = GetRootView();
        if (mDecorView == null)
        {
            return;
        }

        UnsubscribeToPreDraw(mDecorView);
    }
}
