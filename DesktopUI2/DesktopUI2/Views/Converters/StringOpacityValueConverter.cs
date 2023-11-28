using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace DesktopUI2.Views.Converters;

public class StringOpacityValueConverter : IValueConverter
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    if (value == null || string.IsNullOrEmpty(value.ToString()))
    {
      return 0;
    }

    return 1;
  }

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    throw new NotSupportedException();
  }
}
