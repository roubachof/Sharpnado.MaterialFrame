using Android.Content;
using Android.Graphics;

namespace Sharpnado.MaterialFrame.Droid;

/// <summary>
/// Fast CPU-based blur implementation using the StackBlur algorithm.
/// Works on all Android versions and provides good performance without GPU acceleration.
/// Based on Mario Klingemann's Stack Blur Algorithm.
/// </summary>
public class AndroidStackBlur : IBlurImpl
{
    private int _radius;

    public bool Prepare(Context context, Bitmap buffer, float radius)
    {
        _radius = Math.Max(1, (int)Math.Round(radius));
        return true;
    }

    public void Blur(Bitmap input, Bitmap output)
    {
        if (input.Width != output.Width || input.Height != output.Height)
        {
            InternalLogger.Error("AndroidStackBlur", "Input and output bitmaps must have the same dimensions");
            return;
        }

        try
        {
            StackBlurBitmap(input, output, _radius);
        }
        catch (Exception ex)
        {
            InternalLogger.Error("AndroidStackBlur", ex);
        }
    }

    public void Release()
    {
        // No resources to release
    }

    /// <summary>
    /// Stack Blur Algorithm by Mario Klingemann.
    /// Optimized for performance while maintaining good visual quality.
    /// </summary>
    private static void StackBlurBitmap(Bitmap input, Bitmap output, int radius)
    {
        int width = input.Width;
        int height = input.Height;

        int[] inputPixels = new int[width * height];
        int[] outputPixels = new int[width * height];

        input.GetPixels(inputPixels, 0, width, 0, 0, width, height);

        int wm = width - 1;
        int hm = height - 1;
        int wh = width * height;
        int div = radius + radius + 1;

        int[] r = new int[wh];
        int[] g = new int[wh];
        int[] b = new int[wh];
        int[] a = new int[wh];
        int rsum, gsum, bsum, asum, x, y, i, p, yp, yi, yw;
        int[] vmin = new int[Math.Max(width, height)];

        int divsum = (div + 1) >> 1;
        divsum *= divsum;
        int[] dv = new int[256 * divsum];
        for (i = 0; i < 256 * divsum; i++)
        {
            dv[i] = (i / divsum);
        }

        yw = yi = 0;

        int[][] stack = new int[div][];
        for (int k = 0; k < div; k++)
        {
            stack[k] = new int[4];
        }

        int stackpointer;
        int stackstart;
        int[] sir;
        int rbs;
        int r1 = radius + 1;
        int routsum, goutsum, boutsum, aoutsum;
        int rinsum, ginsum, binsum, ainsum;

        // Horizontal blur
        for (y = 0; y < height; y++)
        {
            rinsum = ginsum = binsum = ainsum = routsum = goutsum = boutsum = aoutsum = rsum = gsum = bsum = asum = 0;
            for (i = -radius; i <= radius; i++)
            {
                p = inputPixels[yi + Math.Min(wm, Math.Max(i, 0))];
                sir = stack[i + radius];
                sir[0] = (p >> 16) & 0xff;
                sir[1] = (p >> 8) & 0xff;
                sir[2] = p & 0xff;
                sir[3] = (p >> 24) & 0xff;
                rbs = r1 - Math.Abs(i);
                rsum += sir[0] * rbs;
                gsum += sir[1] * rbs;
                bsum += sir[2] * rbs;
                asum += sir[3] * rbs;
                if (i > 0)
                {
                    rinsum += sir[0];
                    ginsum += sir[1];
                    binsum += sir[2];
                    ainsum += sir[3];
                }
                else
                {
                    routsum += sir[0];
                    goutsum += sir[1];
                    boutsum += sir[2];
                    aoutsum += sir[3];
                }
            }
            stackpointer = radius;

            for (x = 0; x < width; x++)
            {
                r[yi] = dv[rsum];
                g[yi] = dv[gsum];
                b[yi] = dv[bsum];
                a[yi] = dv[asum];

                rsum -= routsum;
                gsum -= goutsum;
                bsum -= boutsum;
                asum -= aoutsum;

                stackstart = stackpointer - radius + div;
                sir = stack[stackstart % div];

                routsum -= sir[0];
                goutsum -= sir[1];
                boutsum -= sir[2];
                aoutsum -= sir[3];

                if (y == 0)
                {
                    vmin[x] = Math.Min(x + radius + 1, wm);
                }
                p = inputPixels[yw + vmin[x]];

                sir[0] = (p >> 16) & 0xff;
                sir[1] = (p >> 8) & 0xff;
                sir[2] = p & 0xff;
                sir[3] = (p >> 24) & 0xff;

                rinsum += sir[0];
                ginsum += sir[1];
                binsum += sir[2];
                ainsum += sir[3];

                rsum += rinsum;
                gsum += ginsum;
                bsum += binsum;
                asum += ainsum;

                stackpointer = (stackpointer + 1) % div;
                sir = stack[stackpointer % div];

                routsum += sir[0];
                goutsum += sir[1];
                boutsum += sir[2];
                aoutsum += sir[3];

                rinsum -= sir[0];
                ginsum -= sir[1];
                binsum -= sir[2];
                ainsum -= sir[3];

                yi++;
            }
            yw += width;
        }

        // Vertical blur
        for (x = 0; x < width; x++)
        {
            rinsum = ginsum = binsum = ainsum = routsum = goutsum = boutsum = aoutsum = rsum = gsum = bsum = asum = 0;
            yp = -radius * width;
            for (i = -radius; i <= radius; i++)
            {
                yi = Math.Max(0, yp) + x;

                sir = stack[i + radius];

                sir[0] = r[yi];
                sir[1] = g[yi];
                sir[2] = b[yi];
                sir[3] = a[yi];

                rbs = r1 - Math.Abs(i);

                rsum += r[yi] * rbs;
                gsum += g[yi] * rbs;
                bsum += b[yi] * rbs;
                asum += a[yi] * rbs;

                if (i > 0)
                {
                    rinsum += sir[0];
                    ginsum += sir[1];
                    binsum += sir[2];
                    ainsum += sir[3];
                }
                else
                {
                    routsum += sir[0];
                    goutsum += sir[1];
                    boutsum += sir[2];
                    aoutsum += sir[3];
                }

                if (i < hm)
                {
                    yp += width;
                }
            }
            yi = x;
            stackpointer = radius;
            for (y = 0; y < height; y++)
            {
                outputPixels[yi] = (dv[asum] << 24) | (dv[rsum] << 16) | (dv[gsum] << 8) | dv[bsum];

                rsum -= routsum;
                gsum -= goutsum;
                bsum -= boutsum;
                asum -= aoutsum;

                stackstart = stackpointer - radius + div;
                sir = stack[stackstart % div];

                routsum -= sir[0];
                goutsum -= sir[1];
                boutsum -= sir[2];
                aoutsum -= sir[3];

                if (x == 0)
                {
                    vmin[y] = Math.Min(y + r1, hm) * width;
                }
                p = x + vmin[y];

                sir[0] = r[p];
                sir[1] = g[p];
                sir[2] = b[p];
                sir[3] = a[p];

                rinsum += sir[0];
                ginsum += sir[1];
                binsum += sir[2];
                ainsum += sir[3];

                rsum += rinsum;
                gsum += ginsum;
                bsum += binsum;
                asum += ainsum;

                stackpointer = (stackpointer + 1) % div;
                sir = stack[stackpointer];

                routsum += sir[0];
                goutsum += sir[1];
                boutsum += sir[2];
                aoutsum += sir[3];

                rinsum -= sir[0];
                ginsum -= sir[1];
                binsum -= sir[2];
                ainsum -= sir[3];

                yi += width;
            }
        }

        output.SetPixels(outputPixels, 0, width, 0, 0, width, height);
    }
}
