using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace DesktopUI2.Views.Converters
{
  public class StringEqualsValueConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return value.ToString() == parameter.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotSupportedException();
    }
  }
}

