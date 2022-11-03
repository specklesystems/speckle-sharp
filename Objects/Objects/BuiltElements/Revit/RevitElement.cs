using System;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using System.Linq;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements.Revit
{
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
    
    public RevitElement() { }
  }
}
