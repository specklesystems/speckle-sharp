using Avalonia.Data.Converters;
using System;

namespace DesktopUI2.Views.Converters
{

  public class StreamEditHeightConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
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

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotSupportedException();
    }
  }
}

