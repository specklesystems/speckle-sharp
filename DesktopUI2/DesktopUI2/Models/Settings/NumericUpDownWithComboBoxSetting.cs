using DesktopUI2.Views.Settings;
using System;
using System.Collections.Generic;

namespace DesktopUI2.Models.Settings
{
  public class NumericUpDownWithComboBoxSetting : ISetting
  {
    public string Type => typeof(NumericUpDownWithComboBoxSetting).ToString();
    public string Name { get; set; }
    public string Slug { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }
    public List<string> Values { get; set; }
    public string Selection { get; set; }
    public string Value { get; set; }
    public Type ViewType { get; } = typeof(NumericUpDownWithComboBoxSettingView);
    public string Summary { get; set; }

  }
}
