using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;
using Speckle.Core.Kits;

namespace Objects.Revit
{
  public class ModelCurve : Base, IRevit
  {
    public ICurve baseCurve { get; set; }
    public string lineStyle { get; set; }
    [SchemaIgnore]
    public string elementId { get; set; }
    public Dictionary<string, object> parameters { get; set; }
  }

  public class DetailCurve : Base, IRevit
  {
    public ICurve baseCurve { get; set; }
    public string lineStyle { get; set; }
    [SchemaIgnore]
    public string elementId { get; set; }
    public Dictionary<string, object> parameters { get; set; }
  }

  public class RoomBoundaryLine : Base, IRevit
  {
    public ICurve baseCurve { get; set; }

    [SchemaIgnore]
    public string elementId { get; set; }
    public Dictionary<string, object> parameters { get; set; }

  }
}
