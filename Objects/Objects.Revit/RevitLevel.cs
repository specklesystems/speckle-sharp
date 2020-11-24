using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Revit
{
  [SchemaDescription("Creates a Revit level by elevation and name")]
  public class RevitLevel : Element, ILevel, IRevit
  {
    public string name { get; set; }

    [SchemaOptional]
    [SchemaDescription("If not set, Speckle will try to find an existing level in the revit file you will receive this with the same name. The conversion **will fail** if none is present.")]
    public double? elevation { get; set; }

    [SchemaOptional]
    [SchemaDescription("If true, it creates an associated view in Revit")]
    public bool createView { get; set; }

    [SchemaOptional]
    [DetachProperty]
    public List<Element> elements { get; set; } = new List<Element>();

    [SchemaOptional]
    public Dictionary<string, object> parameters { get; set; }

    [SchemaIgnore]
    public string elementId { get; set; }

  }
}
