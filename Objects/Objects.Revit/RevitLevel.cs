using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;
using Speckle.Core.Kits;

namespace Objects.Revit
{
  [SchemaBuilder("Creates a Revit level by elevation and name")]
  public class RevitLevel : Level, IRevitElement
  {
    public bool createView { get; set; }
    public Dictionary<string, object> parameters { get; set; }

    [SchemaBuilderIgnore]
    public string elementId { get; set; }

    //props not used by levels
    [SchemaBuilderIgnore]
    public string family { get; set; }
    [SchemaBuilderIgnore]
    public string type { get; set; }
    [SchemaBuilderIgnore]
    public RevitLevel level { get; set; }
    [SchemaBuilderIgnore]
    public Dictionary<string, object> typeParameters { get; set; }


  }
}
