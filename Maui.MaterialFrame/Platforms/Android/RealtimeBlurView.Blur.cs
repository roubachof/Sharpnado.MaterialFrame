using Android.Graphics;
using Java.Lang;
using Math = System.Math;

namespace Sharpnado.MaterialFrame.Droid;

public partial class RealtimeBlurView
{
    private void ReleaseBitmap()
    {
        if (!mBitmapToBlur.IsNullOrDisposed())
        {
            mBitmapToBlur.Recycle();
            mBitmapToBlur = null;
        }

        if (!mBlurredBitmap.IsNullOrDisposed())
        {
            mBlurredBitmap.Recycle();
            mBlurredBitmap = null;
        }

        if (!mBlurredBitmapBack.IsNullOrDisposed())
        {
            mBlurredBitmapBack.Recycle();
            mBlurredBitmapBack = null;
        }

        mDisplayedBlurredBitmap = null;
    }

    protected bool Prepare()
    {
        if (mBlurRadius == 0)
        {
            Release();
            return false;
        }

        float downsampleFactor = mDownsampleFactor;
        float radius = mBlurRadius / downsampleFactor;
        if (radius > 25)
        {
            downsampleFactor = downsampleFactor * radius / 25;
            radius = 25;
        }

        int width = Width;
        int height = Height;

        int scaledWidth = Math.Max(1, (int)(width / downsampleFactor));
        int scaledHeight = Math.Max(1, (int)(height / downsampleFactor));

        bool dirty = mDirty;

        if (mBlurringCanvas == null
            || mBlurredBitmap == null
            || mBlurredBitmap.Width != scaledWidth
            || mBlurredBitmap.Height != scaledHeight)
        {
            dirty = true;
            ReleaseBitmap();

            bool r = false;
            try
            {
                mBitmapToBlur = Bitmap.CreateBitmap(scaledWidth, scaledHeight, Bitmap.Config.Argb8888);
                if (mBitmapToBlur == null)
                {
                    return false;
                }

                mBlurringCanvas = new Canvas(mBitmapToBlur);

                InternalLogger.Debug($"BlurView@{GetHashCode()}", () => $"Prepare() => Bitmap.CreateBitmap()");
                mBlurredBitmap = Bitmap.CreateBitmap(scaledWidth, scaledHeight, Bitmap.Config.Argb8888);
                if (mBlurredBitmap == null)
                {
                    return false;
                }

                r = true;
            }
            catch (OutOfMemoryError e)
            {
                // Bitmap.createBitmap() may cause OOM error
                // Simply ignore and fallback
                InternalLogger.Warn($"OutOfMemoryError occured while trying to render the blur view: {e.Message}");
            }
            finally
            {
                if (!r)
                {
                    Release();
                }
            }

            if (!r)
            {
                return false;
            }
        }

        if (dirty)
        {
            InternalLogger.Debug($"BlurView@{GetHashCode()}", () => $"Prepare() => dirty: mBlurImpl.Prepare()");
            if (mBlurImpl.Prepare(Context, mBitmapToBlur, radius))
            {
                mDirty = false;
            }
            else
            {
                return false;
            }
        }

        // Ensure back buffer exists and matches size
        if (mBlurredBitmapBack == null
            || mBlurredBitmapBack.Width != scaledWidth
            || mBlurredBitmapBack.Height != scaledHeight)
        {
            if (!mBlurredBitmapBack.IsNullOrDisposed())
            {
                mBlurredBitmapBack.Recycle();
            }
            mBlurredBitmapBack = Bitmap.CreateBitmap(scaledWidth, scaledHeight, Bitmap.Config.Argb8888);
        }

        return true;
    }

    private void ScheduleAsyncBlur()
    {
        // Skip if blur already in flight or no data
        if (_blurInFlight || mBitmapToBlur == null || Width == 0 || Height == 0)
        {
            return;
        }

        // Change detection: skip blur if content hasn't changed
        int currentHash = ComputeContentHash(mBitmapToBlur);
        if (currentHash == _lastContentHash && mDisplayedBlurredBitmap != null)
        {
            InternalLogger.DebugIf(LogDrawing, $"BlurView@{GetHashCode()}", () => $"ScheduleAsyncBlur => skipping, content unchanged (hash: {currentHash})");
            return;
        }
        _lastContentHash = currentHash;

        lock (_blurLock)
        {
            if (_blurInFlight)
            {
                return;
            }
            _blurInFlight = true;
        }

        // Make a copy of the captured bitmap for background processing
        Bitmap inputCopy = null;
        try
        {
            inputCopy = Bitmap.CreateBitmap(mBitmapToBlur);
        }
        catch (OutOfMemoryError e)
        {
            InternalLogger.Warn($"BlurView@{GetHashCode()}", () => $"ScheduleAsyncBlur => OOM on CreateBitmap copy: {e.Message}");
            lock (_blurLock) { _blurInFlight = false; }
            return;
        }

        var token = _blurCts?.Token ?? CancellationToken.None;

        Task.Run(
            () =>
        {
            try
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                // Ensure back buffer exists and matches input size
                if (mBlurredBitmapBack == null
                    || mBlurredBitmapBack.Width != inputCopy.Width
                    || mBlurredBitmapBack.Height != inputCopy.Height)
                {
                    if (!mBlurredBitmapBack.IsNullOrDisposed())
                    {
                        mBlurredBitmapBack.Recycle();
                    }
                    mBlurredBitmapBack = Bitmap.CreateBitmap(inputCopy.Width, inputCopy.Height, Bitmap.Config.Argb8888);
                }

                // Perform blur on background thread
                InternalLogger.DebugIf(LogDrawing, $"BlurView@{GetHashCode()}", () => $"ScheduleAsyncBlur => running blur on background thread");
                mBlurImpl.Blur(inputCopy, mBlurredBitmapBack);
            }
            catch (System.Exception ex)
            {
                InternalLogger.Error($"BlurView@{GetHashCode()}", () => $"ScheduleAsyncBlur => background blur failed: {ex.Message}");
            }
            finally
            {
                // Dispose the copy to free memory
                if (!inputCopy.IsNullOrDisposed())
                {
                    inputCopy.Recycle();
                    inputCopy = null;
                }

                // Swap displayed buffer on UI thread
                Post(() =>
                {
                    try
                    {
                        // Atomically swap back buffer into display
                        mDisplayedBlurredBitmap = mBlurredBitmapBack;
                        Invalidate();
                        InternalLogger.DebugIf(LogDrawing, $"BlurView@{GetHashCode()}", () => $"ScheduleAsyncBlur => swapped and invalidated");
                    }
                    finally
                    {
                        lock (_blurLock) { _blurInFlight = false; }
                    }
                });
            }
        }, token);
    }

    /// <summary>
    /// Computes a fast hash of the bitmap content by sampling pixels.
    /// Used for change detection to skip blur when content is static.
    /// </summary>
    private int ComputeContentHash(Bitmap bitmap)
    {
        if (bitmap == null || bitmap.Width == 0 || bitmap.Height == 0)
        {
            return 0;
        }

        int width = bitmap.Width;
        int height = bitmap.Height;
        int totalPixels = width * height;

        // Sample pixels in a grid pattern
        int sampleCount = Math.Min(ChangeDetectionSampleCount, totalPixels);
        int step = Math.Max(1, totalPixels / sampleCount);

        try
        {
            // Use GetPixels for fast access (bulk read)
            int[] pixels = new int[totalPixels];
            bitmap.GetPixels(pixels, 0, width, 0, 0, width, height);

            int hash = 17;
            for (int i = 0; i < totalPixels; i += step)
            {
                hash = hash * 31 + pixels[i];
            }

            return hash;
        }
        catch
        {
            // If fails, return 0 to force blur
            return 0;
        }
    }
}
