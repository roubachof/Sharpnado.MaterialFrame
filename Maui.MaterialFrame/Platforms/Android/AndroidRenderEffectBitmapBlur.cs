using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using Color = Android.Graphics.Color;
using View = Android.Views.View;

namespace Sharpnado.MaterialFrame.Droid;

/// <summary>
/// Modern blur implementation using RenderEffect (Android 12+ / API 31+) for bitmap-to-bitmap blurring.
/// This is more efficient than RenderScript and works around the 16KB page size limitation
/// that breaks RenderScript on Android 15+.
/// </summary>
public class AndroidRenderEffectBitmapBlur : IBlurImpl
{
    private float _radius;
    private ImageView? _tempImageView;
    private Context? _context;

    public bool Prepare(Context context, Bitmap buffer, float radius)
    {
        // Check if RenderEffect is available (API 31+)
        if (Build.VERSION.SdkInt < BuildVersionCodes.S)
        {
            return false;
        }

        _context = context;
        _radius = Math.Min(radius, 25f); // RenderEffect has a practical limit of 25dp

        // Create reusable ImageView for rendering
        if (_tempImageView == null)
        {
            _tempImageView = new ImageView(context);
            _tempImageView.SetScaleType(ImageView.ScaleType.FitXy);
            // Enable hardware rendering to ensure RenderEffect is applied
            _tempImageView.SetLayerType(LayerType.Hardware, null);
        }

        return true;
    }

    public void Blur(Bitmap input, Bitmap output)
    {
        if (_tempImageView == null)
        {
            InternalLogger.Error("AndroidRenderEffectBitmapBlur", "Blur() called before successful Prepare()");
            return;
        }

        try
        {
            // Set the input bitmap
            _tempImageView.SetImageBitmap(input);

            // Create and apply blur effect
            var blurEffect = RenderEffect.CreateBlurEffect(
                _radius,
                _radius,
                Shader.TileMode.Clamp!
            );

            _tempImageView.SetRenderEffect(blurEffect);

            // Measure and layout the view
            int width = input.Width;
            int height = input.Height;

            _tempImageView.Measure(
                View.MeasureSpec.MakeMeasureSpec(width, MeasureSpecMode.Exactly),
                View.MeasureSpec.MakeMeasureSpec(height, MeasureSpecMode.Exactly)
            );
            _tempImageView.Layout(0, 0, width, height);

            // Draw the blurred view to the output bitmap
            var canvas = new Canvas(output);
            canvas.DrawColor(Color.Transparent, PorterDuff.Mode.Clear!);
            _tempImageView.Draw(canvas);

            // Clean up for next use
            _tempImageView.SetRenderEffect(null);
            _tempImageView.SetImageBitmap(null);
        }
        catch (Exception ex)
        {
            InternalLogger.Error("AndroidRenderEffectBitmapBlur", $"Error during blur: {ex.Message}");
        }
    }

    public void Release()
    {
        if (_tempImageView != null)
        {
            _tempImageView.SetRenderEffect(null);
            _tempImageView.SetImageBitmap(null);
            _tempImageView.Dispose();
            _tempImageView = null;
        }
        _context = null;
    }

    /// <summary>
    /// Checks if RenderEffect is supported on this device.
    /// </summary>
    public static bool IsSupported()
    {
        return Build.VERSION.SdkInt >= BuildVersionCodes.S;
    }
}
