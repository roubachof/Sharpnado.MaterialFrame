using Android.Graphics;
using Android.Views;
using View = Android.Views.View;

namespace Sharpnado.MaterialFrame.Droid;

/// <summary>
/// OutlineProvider for rounded corners on Android views.
/// </summary>
internal class AndroidOutlineProvider : ViewOutlineProvider
{
    private readonly float _cornerRadius;

    public AndroidOutlineProvider(float cornerRadius)
    {
        _cornerRadius = cornerRadius;
    }

    public override void GetOutline(View view, Outline outline)
    {
        if (_cornerRadius > 0)
        {
            outline.SetRoundRect(0, 0, view.Width, view.Height, _cornerRadius);
        }
        else
        {
            outline.SetRect(0, 0, view.Width, view.Height);
        }
    }
}