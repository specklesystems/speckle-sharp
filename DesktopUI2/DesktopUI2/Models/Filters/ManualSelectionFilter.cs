using System;
using System.Collections.Generic;
using DesktopUI2.Views.Filters;

namespace DesktopUI2.Models.Filters;

public class ManualSelectionFilter : ISelectionFilter
{
  public List<string> Values { get; set; }
  public string Type => typeof(ManualSelectionFilter).ToString();
  public string Name { get; set; } = "Selection";
  public string Slug { get; set; } = "manual";
  public string Icon { get; set; } = "Mouse";
  public string Description { get; set; } = "Manually select model elements.";
  public List<string> Selection { get; set; } = new();
  public Type ViewType { get; } = typeof(ManualFilterView);

  public string Summary
  {
    get
    {
      if (Selection.Count != 0)
      {
        var s = Selection.Count == 1 ? "" : "s";
        return $"{Selection.Count} object{s}";
      }

      return "nothing";
    }
  }
}
