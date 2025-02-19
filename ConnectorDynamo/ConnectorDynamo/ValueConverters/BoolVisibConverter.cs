using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Speckle.ConnectorDynamo.ValueConverters;

/// <summary>
/// return visible if true.
/// can set second parameter to be "opposite" to reverse the functionality
/// </summary>
[ValueConversion(typeof(String), typeof(Visibility))]
public class BoolVisibConverter : IValueConverter
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    if (value == null)
    {
      value = false;
    }

    bool c = (bool)value;

    if (parameter != null && parameter.ToString() == "opposite")
    {
      c = !c;
    }

    return (c) ? Visibility.Visible : Visibility.Collapsed;
  }

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    throw new NotImplementedException();
  }
}
