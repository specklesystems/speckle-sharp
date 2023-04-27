using System;
using DesktopUI2.Views.Settings;

namespace DesktopUI2.Models.Settings;

public class TextBoxSetting : ISetting
{
  public string Type => typeof(CheckBoxSetting).ToString();
  public string Name { get; set; }
  public string Slug { get; set; }
  public string Icon { get; set; }
  public string Description { get; set; }
  public string Selection { get; set; }
  public Type ViewType { get; } = typeof(CheckBoxSettingView);
  public string Summary { get; set; }
}
