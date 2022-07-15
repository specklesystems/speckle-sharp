using DesktopUI2.Views.Settings;
using System;

namespace DesktopUI2.Models.Settings
{
  public class TextBoxSetting : ISetting
  {
    public string Type => typeof(TextBoxSetting).ToString();
    public string Name { get; set; }
    public string Slug { get; set; }
    public string Icon { get; set; }
    public string Watermark { get; set; }
    public string Description { get; set; }
    public string Selection { get; set; }    
    public Type ViewType { get; } = typeof(TextBoxSettingView);
    public string Summary { get; set; }

  }
}
