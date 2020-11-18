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
    public double elevation { get; set; }
    public bool createView { get; set; }
    public List<Element> elements { get; set; } = new List<Element>();

    public Dictionary<string, object> parameters { get; set; }

    [SchemaIgnore]
    public string elementId { get; set; }

  }
}
