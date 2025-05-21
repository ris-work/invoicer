#if WINDOWS
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using CommonUi;
using Eto.Drawing;

namespace CommonUi
{
    public class EtoColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Eto.Drawing.Color etoColor)
            {
                // Convert using the extension method above.
                System.Windows.Media.Color mediaColor = etoColor.ToMediaColor();
                return new SolidColorBrush(mediaColor);
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture
        )
        {
            throw new NotImplementedException();
        }
    }
}
#endif
