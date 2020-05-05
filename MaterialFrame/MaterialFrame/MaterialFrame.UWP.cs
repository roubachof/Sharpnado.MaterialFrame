using Xamarin.Forms;

namespace Sharpnado.MaterialFrame
{
    public partial class MaterialFrame
    {
        public static readonly BindableProperty UwpBlurOverlayColorProperty = BindableProperty.Create(
            nameof(UwpBlurOverlayColor),
            typeof(Color),
            typeof(MaterialFrame),
            defaultValueCreator: _ => Color.Default);

        /// <summary>
        /// UWP only.
        /// Changes the overlay color over the blur (should be a transparent color, obviously).
        /// If not set, the different blur style styles take over.
        /// </summary>
        public Color UwpBlurOverlayColor
        {
            get => (Color)GetValue(UwpBlurOverlayColorProperty);
            set => SetValue(UwpBlurOverlayColorProperty, value);
        }
    }
}
