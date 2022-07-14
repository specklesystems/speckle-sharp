using DesktopUI2.Views.Settings;
using System;

namespace DesktopUI2.Models.Settings
{
  public class FieldBoxSetting : ISetting
  {
    public string Type => typeof(FieldBoxSetting).ToString();
    public string Name { get; set; }
    public string Slug { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }
    public string Selection { get; set; }
    public Type ViewType { get; } = typeof(FieldBoxSettingView);
    public string Summary { get; set; }

  }
}
