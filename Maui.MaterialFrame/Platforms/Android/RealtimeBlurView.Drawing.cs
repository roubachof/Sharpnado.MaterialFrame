using Android.Graphics;
using Color = Android.Graphics.Color;
using RectF = Android.Graphics.RectF;

namespace Sharpnado.MaterialFrame.Droid;

public partial class RealtimeBlurView
{
    public override void Draw(Canvas canvas)
    {
        if (mIsRendering)
        {
            InternalLogger.DebugIf(LogDrawing, $"BlurView@{GetHashCode()}", () => $"Draw() => skipping during blur capture");

            // During blur capture, skip drawing this view but don't stop the traversal
            // This allows sibling views (like the background image/label) to be drawn
            // Only throw StopException if explicitly enabled (for advanced scenarios)
            if (AndroidMaterialFrameHandler.ThrowStopExceptionOnDraw)
            {
                throw new StopException();
            }

            // Don't call base.Draw() - just skip this view entirely during capture
            return;
        }

        if (RENDERING_COUNT > 0)
        {
            InternalLogger.DebugIf(LogDrawing, $"BlurView@{GetHashCode()}", () => $"Draw() => Doesn't support blurview overlap on another blurview");

            // Doesn't support blurview overlap on another blurview
            // Also skip drawing to prevent infinite recursion
        }
        else
        {
            InternalLogger.DebugIf(LogDrawing, $"BlurView@{GetHashCode()}", () => $"Draw() => calling base draw");
            base.Draw(canvas);
        }
    }

    protected override void OnDraw(Canvas canvas)
    {
        base.OnDraw(canvas);

        InternalLogger.Debug($"BlurView@{GetHashCode()}", () => $"OnDraw(formsId: {_formsId})");
        DrawRoundedBlurredBitmap(canvas, mDisplayedBlurredBitmap, mOverlayColor);
    }

    private void DrawRoundedBlurredBitmap(Canvas canvas, Bitmap blurredBitmap, int overlayColor)
    {
        if (blurredBitmap != null)
        {
            InternalLogger.DebugIf(
                LogDrawing,
                $"BlurView@{GetHashCode()}",
                () => $"DrawRoundedBlurredBitmap( mCornerRadius: {mCornerRadius}, mOverlayColor: {mOverlayColor} )");

            var mRectF = new RectF { Right = Width, Bottom = Height };

            mPaint.Reset();
            mPaint.AntiAlias = true;
            BitmapShader shader = new BitmapShader(blurredBitmap, Shader.TileMode.Clamp, Shader.TileMode.Clamp);
            Matrix matrix = new Matrix();
            matrix.PostScale(mRectF.Width() / blurredBitmap.Width, mRectF.Height() / blurredBitmap.Height);
            shader.SetLocalMatrix(matrix);
            mPaint.SetShader(shader);
            canvas.DrawRoundRect(mRectF, mCornerRadius, mCornerRadius, mPaint);

            mPaint.Reset();
            mPaint.AntiAlias = true;
            mPaint.Color = new Color(overlayColor);
            canvas.DrawRoundRect(mRectF, mCornerRadius, mCornerRadius, mPaint);
        }
    }
}
