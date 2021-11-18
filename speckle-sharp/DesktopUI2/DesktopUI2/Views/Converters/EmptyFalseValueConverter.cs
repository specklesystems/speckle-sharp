using Avalonia.Data.Converters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace DesktopUI2.Views.Converters
{

  public class EmptyFalseValueConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return (int)value == 0 ? false : true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotSupportedException();
    }
  }
}

