using System;
using System.Globalization;
using DesktopUI2.Views.Settings;

namespace DesktopUI2.Models.Settings;

public class NumericSetting : ISetting
{
  // Default type for Avalonia NumericUpDown is Double
  public double Value
  {
    get => double.Parse(Selection);
    set => Selection = value.ToString(CultureInfo.InvariantCulture);
  }

  // Spinner is the flag for both activating and making the increment rockers visible. Defaults off.
  public bool Spinner { get; set; } = false;

  // Increment is set to 1000s of 1 unit to accomodate likely accuracies between m and millimeters. Connnectors can override.
  public double Increment { get; set; } = 0.001;
  public string Type => typeof(NumericSetting).ToString();
  public string Name { get; set; }
  public string Slug { get; set; }
  public string Icon { get; set; }
  public string Description { get; set; }
  public string Selection { get; set; }
  public Type ViewType { get; } = typeof(NumericSettingView);
  public string Summary { get; set; }
}
