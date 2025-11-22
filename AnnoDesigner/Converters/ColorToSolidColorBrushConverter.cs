using System;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;
using System.Windows.Media;

namespace AnnoDesigner.Converters
{
    [ValueConversion(typeof(object), typeof(SolidColorBrush))]
    public class ColorToSolidColorBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            // If it's already a Color
            if (value is Color color)
            {
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                return brush;
            }

            // If it's a SolidColorBrush already
            if (value is SolidColorBrush scb)
            {
                return scb;
            }

            // Try to handle types that expose a MediaColor or Color property (e.g., SerializableColor)
            var t = value.GetType();

            // common property names
            var prop = t.GetProperty("MediaColor") ?? t.GetProperty("Color") ?? t.GetProperty("Value");
            if (prop != null)
            {
                var propValue = prop.GetValue(value);
                if (propValue is Color color2)
                {
                    var brush = new SolidColorBrush(color2);
                    brush.Freeze();
                    return brush;
                }
            }

            // If it's a string like "#FFAABBCC" try parse
            if (value is string s)
            {
                try
                {
                    var parsed = (Color)ColorConverter.ConvertFromString(s);
                    var brush = new SolidColorBrush(parsed);
                    brush.Freeze();
                    return brush;
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
