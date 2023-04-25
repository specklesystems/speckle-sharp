using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Revit;

/// <summary>
/// A generic Revit element for which we don't have direct conversions
/// </summary>
public class RevitElement : Base, IDisplayValue<List<Mesh>>
{
  public string family { get; set; }
  public string type { get; set; }
  public string category { get; set; }
  public Base parameters { get; set; }
  public string elementId { get; set; }

  [DetachProperty]
  public List<Mesh> displayValue { get; set; }
}
