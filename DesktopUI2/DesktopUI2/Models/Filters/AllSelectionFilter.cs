using DesktopUI2.Views.Filters;
using System;
using System.Collections.Generic;

namespace DesktopUI2.Models.Filters
{
  public class AllSelectionFilter : ISelectionFilter
  {
    public string Type => typeof(AllSelectionFilter).ToString();
    public string Name { get; set; }
    public string Slug { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }
    public List<string> Selection { get; set; } = new List<string>();
    public Type ViewType { get; } = typeof(AllFilterView);

    public string Summary
    {
      get
      {
        return "everything";
      }
    }
  }
}
