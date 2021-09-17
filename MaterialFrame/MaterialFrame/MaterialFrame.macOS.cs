using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Sharpnado.MaterialFrame
{
    public partial class MaterialFrame
    {
        public static readonly BindableProperty macOSBehindWindowBlurProperty = BindableProperty.Create(
            nameof(macOSBehindWindowBlur),
            typeof(bool),
            typeof(MaterialFrame),
            defaultValueCreator: _ => false);

        /// <summary>
        /// macOS only.
        /// BehindWindow reveals the desktop wallpaper and other windows that are behind the currently active app.
        /// If not set, the default in app WithinWindow take over.
        /// </summary>
        public bool macOSBehindWindowBlur
        {
            get => (bool)GetValue(macOSBehindWindowBlurProperty);
            set => SetValue(macOSBehindWindowBlurProperty, value);
        }
    }
}
