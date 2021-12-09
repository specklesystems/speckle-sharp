using Avalonia.Controls;
using DesktopUI2.Views.Filters;
using System;
using System.Collections.Generic;
using System.Text;

namespace DesktopUI2.Models.Settings
{
  public class DropdownSetting : ISetting
  {
    public string Type => typeof(DropdownSetting).ToString();

    public string Name { get; set; }
    public string Slug { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }

    public List<string> Values { get; set; }
    public string Selection { get; set; } = "Default";

    public UserControl View { get; set; } = new ListFilterView();

    public string Summary
    {
      get
      {
        return Selection;
      }
    }


  }
}
