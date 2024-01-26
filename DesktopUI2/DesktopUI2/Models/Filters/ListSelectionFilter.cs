using System;
using System.Collections.Generic;
using DesktopUI2.Views.Filters;

namespace DesktopUI2.Models.Filters;

public class ListSelectionFilter : ISelectionFilter
{
  public List<string> Values { get; set; }
  public string Type => typeof(ListSelectionFilter).ToString();
  public string Name { get; set; }
  public string Slug { get; set; }
  public string Icon { get; set; }
  public string Description { get; set; }
  public List<string> Selection { get; set; } = new();
  public Type ViewType { get; } = typeof(ListFilterView);

  public string Summary
  {
    get
    {
      if (Selection.Count != 0)
      {
        return string.Join(", ", Selection);
      }

      return "nothing";
    }
  }
}
