using Avalonia.Data.Converters;
using System;

namespace DesktopUI2.Views.Converters
{

  public class StringOpacityValueConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (value == null || String.IsNullOrEmpty(value.ToString()))
        return 0;
      return 1;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotSupportedException();
    }
  }
}

