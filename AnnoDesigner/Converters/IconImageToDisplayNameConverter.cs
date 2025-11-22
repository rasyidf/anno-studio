using System;
using System.Globalization;
using System.Windows.Data;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Helper;

namespace AnnoDesigner.Converters
{
    [ValueConversion(typeof(IconImage), typeof(string))]
    public class IconImageToDisplayNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is not IconImage iconImage ? value : iconImage.NameForLanguage(Commons.Instance.CurrentLanguageCode);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
