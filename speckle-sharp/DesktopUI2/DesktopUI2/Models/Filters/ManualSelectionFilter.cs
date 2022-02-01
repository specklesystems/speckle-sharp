using Avalonia.Controls;
using DesktopUI2.Views.Filters;
using System;
using System.Collections.Generic;
using System.Text;

namespace DesktopUI2.Models.Filters
{
  public class ManualSelectionFilter : ISelectionFilter
  {
    public string Type => typeof(ManualSelectionFilter).ToString();

    public string Name { get; set; } = "Selection";
    public string Slug { get; set; } = "manual";
    public string Icon { get; set; } = "Mouse";
    public string Description { get; set; } = "Manually select model elements.";

    public List<string> Values { get; set; }
    public List<string> Selection { get; set; } = new List<string>();

    public UserControl View { get; set; } = new ManualFilterView();

    public string Summary
    {
      get
      {
        if (Selection.Count != 0)
        {
          return Selection.Count + " objects selected.";
        }
        else
        {
          return "Nothing selected.";
        }
      }
    }
  }
}
