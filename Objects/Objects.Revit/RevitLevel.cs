using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;
using Speckle.Core.Kits;

namespace Objects.Revit
{
  [SchemaBuilder("Creates a Revit level by elevation and name")]
  public class RevitLevel : ILevel, IRevit
  {
    public string name { get; set; }
    public double elevation { get; set; }
    public bool createView { get; set; }
    public Dictionary<string, object> parameters { get; set; }

    [SchemaBuilderIgnore]
    public string elementId { get; set; }

  }
}
