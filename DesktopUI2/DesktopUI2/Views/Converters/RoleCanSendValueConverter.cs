using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace DesktopUI2.Views.Converters
{
  public class RoleCanSendValueConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return value!=null && value.ToString() != "stream:reviewer";
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotSupportedException();
    }
  }
}

