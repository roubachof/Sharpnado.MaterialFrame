using Xamarin.Forms;

namespace Sharpnado.MaterialFrame
{
    public partial class MaterialFrame
    {
        public static readonly BindableProperty MacOSBehindWindowBlurProperty = BindableProperty.Create(
            nameof(MacOSBehindWindowBlur),
            typeof(bool),
            typeof(MaterialFrame),
            defaultValueCreator: _ => false);

        /// <summary>
        /// macOS only.
        /// BehindWindow reveals the desktop wallpaper and other windows that are behind the currently active app.
        /// If not set, the default in app WithinWindow take over.
        /// </summary>
        public bool MacOSBehindWindowBlur
        {
            get => (bool)GetValue(MacOSBehindWindowBlurProperty);
            set => SetValue(MacOSBehindWindowBlurProperty, value);
        }
    }
}
