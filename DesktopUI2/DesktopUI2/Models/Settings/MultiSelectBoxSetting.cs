using DesktopUI2.Views.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DesktopUI2.Models.Settings
{
  public class MultiSelectBoxSetting : ISetting
  {
    public string Type => typeof(MultiSelectBoxSetting).ToString();
    public string Name { get; set; }
    public string Slug { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }
    public List<string> Values { get; set; }
    public string Selection { get; set; }
    public ObservableCollection<string> Selections { get; set; }
    public Type ViewType { get; } = typeof(MultiSelectBoxSettingView);
    public string Summary { get; set; }

  }
}
