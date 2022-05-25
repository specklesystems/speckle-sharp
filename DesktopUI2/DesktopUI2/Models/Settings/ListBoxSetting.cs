using DesktopUI2.Views.Settings;
using System;
using System.Collections.Generic;

namespace DesktopUI2.Models.Settings
{
  public class ListBoxSetting : ISetting
  {
    public string Type => typeof(ListBoxSetting).ToString();
    public string Name { get; set; }
    public string Slug { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }
    public List<string> Values { get; set; }
    public string Selection { get; set; }
    public Type ViewType { get; } = typeof(ListBoxSettingView);
    public string Summary { get; set; }

  }
}
