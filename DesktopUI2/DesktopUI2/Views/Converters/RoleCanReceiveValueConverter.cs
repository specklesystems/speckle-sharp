#nullable enable
using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Speckle.Core.Api.GraphQL;

namespace DesktopUI2.Views.Converters;

public class RoleCanReceiveValueConverter : IValueConverter
{
  public bool Convert(object? value)
  {
    return value switch
    {
      StreamRoles.STREAM_OWNER => true,
      StreamRoles.STREAM_CONTRIBUTOR => true,
      _ => false
    };
  }

  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => Convert(value);

  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    throw new NotSupportedException();
  }
}

public sealed class RoleCanNotReceiveValueConverter : IValueConverter
{
  private readonly RoleCanReceiveValueConverter _inner = new();

  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    return !_inner.Convert(value);
  }

  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    throw new NotSupportedException();
  }
}

public sealed class RoleReceiveErrorMessageConverter : IValueConverter
{
  private readonly RoleCanReceiveValueConverter _inner = new();

  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    if (!_inner.Convert(value))
    {
      return "You do not have access to send or receive models on this project";
    }
    else
    {
      return null;
    }
  }

  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    throw new NotSupportedException();
  }
}
