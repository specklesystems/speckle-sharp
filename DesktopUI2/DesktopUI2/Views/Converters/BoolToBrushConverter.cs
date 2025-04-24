#nullable enable
using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DesktopUI2.Views.Converters;

/// <summary>
/// converter for HomeViewModel to "grey out" stream buttons without disabling them (since tooltips, and "open in web" buttons would also be disabled)
/// </summary>
public sealed class BoolToBrushConverter : IValueConverter
{
  public IBrush TrueBrush { get; set; } = Brushes.White;
  public IBrush FalseBrush { get; set; } = Brushes.Gray;

  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    if (value is bool b)
      return b ? TrueBrush : FalseBrush;

    return FalseBrush;
  }

  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
    throw new NotSupportedException();
}
