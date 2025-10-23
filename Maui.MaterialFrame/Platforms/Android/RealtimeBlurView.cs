//------------------------------------------------------------------------------
//
// https://github.com/mmin18/RealtimeBlurView
// Latest commit    82df352     on 24 May 2019
//
// Copyright 2016 Tu Yimin (http://github.com/mmin18)
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
//------------------------------------------------------------------------------
// Adapted to csharp and Xamarin.Forms by Jean-Marie Alfonsi
//------------------------------------------------------------------------------

using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Views;
using Paint = Android.Graphics.Paint;
using Rect = Android.Graphics.Rect;
using View = Android.Views.View;

namespace Sharpnado.MaterialFrame.Droid;

/**
 * A realtime blurring overlay (like iOS UIVisualEffectView). Just put it above
 * the view you want to blur and it doesn't have to be in the same ViewGroup
 * <ul>
 * <li>realtimeBlurRadius (10dp)</li>
 * <li>realtimeDownsampleFactor (4)</li>
 * <li>realtimeOverlayColor (#aaffffff)</li>
 * </ul>
 */
public partial class RealtimeBlurView : View
{
    internal const bool LogDrawing = false;

    private static int _realtimeBlurViewInstanceCount;

    private float mDownsampleFactor; // default 4
    private int mOverlayColor; // default #aaffffff
    private float mBlurRadius; // default 10dp (0 < r <= 25)
    private float mCornerRadius; // default 0

    private readonly IBlurImpl mBlurImpl;
    private readonly string _formsId;

    private bool mDirty;
    private Bitmap mBitmapToBlur, mBlurredBitmap;
    private Canvas mBlurringCanvas;
    private bool mIsRendering;
    private Paint mPaint;
    private Rect mRectSrc = new Rect(), mRectDst = new Rect();

    // Async blur state
    private volatile bool _blurInFlight;
    private readonly object _blurLock = new object();
    private CancellationTokenSource _blurCts;

    // Double-buffered blurred outputs (back buffer and displayed buffer)
    private Bitmap mBlurredBitmapBack;
    private Bitmap mDisplayedBlurredBitmap;

    // Change detection to skip blur when background is static
    private int _lastContentHash;
    private const int ChangeDetectionSampleCount = 64; // Sample 64 pixels

    private JniWeakReference<View> _weakDecorView;

    // If the view is on different root view (usually means we are on a PopupWindow),
    // we need to manually call invalidate() in onPreDraw(), otherwise we will not be able to see the changes
    private bool mDifferentRoot;

    private bool _isContainerShown;
    private bool _autoUpdate;
    private bool _isDetached;

    private static int RENDERING_COUNT;
    private static int BLUR_IMPL;

    private readonly PreDrawListener preDrawListener;

    public RealtimeBlurView(Context context, string formsId)
        : base(context)
    {
        mBlurImpl = GetBlurImpl();
        mPaint = new Paint();

        _formsId = formsId;
        _isContainerShown = true;
        _autoUpdate = true;

        preDrawListener = new PreDrawListener(this);
        _blurCts = new CancellationTokenSource();

        _realtimeBlurViewInstanceCount++;
        InternalLogger.Debug("RealtimeBlurView", $"Constructor => Active instances: {_realtimeBlurViewInstanceCount}");
    }

    public RealtimeBlurView(IntPtr javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }

    protected override void JavaFinalize()
    {
        base.JavaFinalize();
        _realtimeBlurViewInstanceCount--;
        InternalLogger.Debug("RealtimeBlurView", $"JavaFinalize() => Active instances: {_realtimeBlurViewInstanceCount}");
    }

    protected IBlurImpl GetBlurImpl()
    {
        // Use StackBlur for all Android versions:
        // - Works on all devices (no API level restrictions)
        // - No 16KB page issues (unlike RenderScript on Android 15+)
        // - Good performance for real-time blur (~15-25ms for 500x500)
        // - Simple and maintainable
        BLUR_IMPL = 2; // StackBlur
        return new AndroidStackBlur();
    }

    public void SetCornerRadius(float radius)
    {
        if (mCornerRadius != radius)
        {
            mCornerRadius = radius;
            mDirty = true;
            Invalidate();
        }
    }

    public void SetDownsampleFactor(float factor)
    {
        if (factor <= 0)
        {
            throw new ArgumentException("Downsample factor must be greater than 0.");
        }

        if (mDownsampleFactor != factor)
        {
            mDownsampleFactor = factor;
            mDirty = true; // may also change blur radius
            ReleaseBitmap();
            Invalidate();
        }
    }

    public void SetBlurRadius(float radius, bool invalidate = true)
    {
        if (mBlurRadius != radius)
        {
            mBlurRadius = radius;
            mDirty = true;
            if (invalidate)
            {
                Invalidate();
            }
        }
    }

    public void SetOverlayColor(int color, bool invalidate = true)
    {
        if (mOverlayColor != color)
        {
            mOverlayColor = color;
            if (invalidate)
            {
                Invalidate();
            }
        }
    }

    public void SetRootView(View rootView)
    {
        var mDecorView = GetRootView();
        if (mDecorView != rootView)
        {
            UnsubscribeToPreDraw(mDecorView);
            _weakDecorView = new JniWeakReference<View>(rootView);

            if (IsAttachedToWindow)
            {
                OnAttached(rootView);
            }
        }
    }

    private View GetRootView()
    {
        View mDecorView = null;
        _weakDecorView?.TryGetTarget(out mDecorView);
        return mDecorView;
    }

    private class StopException : System.Exception;
}
