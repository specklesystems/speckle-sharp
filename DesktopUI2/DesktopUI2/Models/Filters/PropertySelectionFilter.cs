using System;
using System.Collections.Generic;
using DesktopUI2.Views.Filters;

namespace DesktopUI2.Models.Filters;

public class PropertySelectionFilter : ISelectionFilter
{
  public List<string> Values { get; set; }
  public List<string> Operators { get; set; }
  public string PropertyName { get; set; }
  public string PropertyValue { get; set; }
  public string PropertyOperator { get; set; }
  public string Type => typeof(PropertySelectionFilter).ToString();
  public string Name { get; set; }
  public string Slug { get; set; }
  public string Icon { get; set; }
  public string Description { get; set; }
  public List<string> Selection { get; set; } = new();

  public Type ViewType { get; } = typeof(PropertyFilterView);

  public string Summary => $"{PropertyName} {PropertyOperator} {PropertyValue}";
}
