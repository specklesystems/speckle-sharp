using Objects.BuiltElements;
using Speckle.Core.Kits;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.Revit
{
  /// <summary>
  /// Represents a generic Revit element that has type, family, level and parameters
  /// </summary>

  [SchemaVisibility(Visibility.Hidden)]
  public class RevitElement : Element, IRevit
  {
    public string type { get; set; }
    public string family { get; set; }
    public RevitLevel level { get; set; }

    [SchemaVisibility(Visibility.Hidden)]
    public string elementId { get; set; }
    public Dictionary<string, object> parameters { get; set; }
    public Dictionary<string, object> typeParameters { get; set; }
  }
}
