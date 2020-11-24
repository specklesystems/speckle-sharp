using Objects.BuiltElements;
using Speckle.Core.Kits;
using System.Collections.Generic;

namespace Objects.Revit
{
  /// <summary>
  /// Represents a generic Revit element that has type, family, level and parameters
  /// </summary>

  [SchemaIgnore]
  public class RevitElement : Element, IRevit
  {
    public string family { get; set; }

    public string type { get; set; }

    public ILevel level { get; set; }

    [SchemaOptional]
    public Dictionary<string, object> parameters { get; set; }

    [SchemaOptional]
    public Dictionary<string, object> typeParameters { get; set; }

    [SchemaIgnore]
    public string elementId { get; set; }
  }

}
