using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Speckle.Core.Api.GraphQL;

namespace DesktopUI2.Views.Converters;

public class RoleValueConverter : IValueConverter
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    return value switch
    {
      null => "View in web only", //Public streams
      StreamRoles.STREAM_OWNER => "Project Owner",
      StreamRoles.STREAM_CONTRIBUTOR => "Can Edit",
      StreamRoles.STREAM_REVIEWER => "View in web only",
      _ => value,
    };
  }

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    throw new NotImplementedException();
  }
}
