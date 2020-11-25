using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Revit
{
  [SchemaDescription("Creates a new Revit level by elevation and name")]
  public class RevitLevel : Base, IBaseRevitElement, IRevitHasParameters, ILevel
  {
    public string name { get; set; }

    [SchemaDescription("Elevation of the level")]
    public double elevation { get; set; }

    [SchemaOptional]
    [SchemaDescription("If true, it creates an associated view in Revit")]
    public bool createView { get; set; }

    [SchemaOptional]
    public Dictionary<string, object> parameters { get; set; }

    [SchemaIgnore]
    public string elementId { get; set; }

  }

  [SchemaDescription("An existing Revit level")]
  public class RevitLevelByName : IBaseRevitElement, ILevel
  {
    public string name { get; set; }

    [SchemaIgnore]
    public double elevation { get; set; }

    [SchemaIgnore]
    public string elementId { get; set; }
  }
}
