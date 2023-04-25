using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace DesktopUI2.Views.Converters;

public class StreamEditHeightConverter : IValueConverter
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    try
    {
      return (double)value - 80;
    }
    catch (Exception e)
    {
      return 1000;
    }
  }

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    throw new NotSupportedException();
  }
}
