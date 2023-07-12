namespace Sharpnado.MaterialFrame
{
    public partial class MaterialFrame
    {
        public static readonly BindableProperty WinUIBlurOverlayColorProperty = BindableProperty.Create(
            nameof(WinUIBlurOverlayColor),
            typeof(Color),
            typeof(MaterialFrame),
            defaultValueCreator: _ => Colors.Transparent);

        public static readonly BindableProperty WinUIHostBackdropBlurProperty = BindableProperty.Create(
            nameof(WinUIHostBackdropBlur),
            typeof(bool),
            typeof(MaterialFrame),
            defaultValueCreator: _ => false);

        /// <summary>
        /// WinUI only.
        /// Changes the overlay color over the blur (should be a transparent color, obviously).
        /// If not set, the different blur style styles take over.
        /// </summary>
        public Color WinUIBlurOverlayColor
        {
            get => (Color)GetValue(WinUIBlurOverlayColorProperty);
            set => SetValue(WinUIBlurOverlayColorProperty, value);
        }

        /// <summary>
        /// Not supported in WinUI 3: https://github.com/microsoft/microsoft-ui-xaml/issues/6618
        /// HostBackdropBlur reveals the desktop wallpaper and other windows that are behind the currently active app.
        /// If not set, the default in app BackdropBlur take over.
        /// </summary>
        public bool WinUIHostBackdropBlur
        {
            get => (bool)GetValue(WinUIHostBackdropBlurProperty);
            set => SetValue(WinUIHostBackdropBlurProperty, value);
        }
    }
}
