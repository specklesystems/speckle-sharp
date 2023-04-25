using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace DesktopUI2.Views.Converters;

public class RoleCanSendValueConverter : IValueConverter
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    return value != null && value.ToString() != "stream:reviewer";
  }

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    throw new NotSupportedException();
  }
}
