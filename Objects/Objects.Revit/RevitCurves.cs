using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;
using Speckle.Core.Kits;

namespace Objects.Revit
{
  public class ModelCurve : Base, IBaseRevitElement
  {
    public ICurve baseCurve { get; set; }
    public string lineStyle { get; set; }

    [SchemaOptional]
    public Dictionary<string, object> parameters { get; set; }

    [SchemaIgnore]
    public string elementId { get; set; }
  }

  public class DetailCurve : Base, IBaseRevitElement
  {
    public ICurve baseCurve { get; set; }
    public string lineStyle { get; set; }

    [SchemaOptional]
    public Dictionary<string, object> parameters { get; set; }

    [SchemaIgnore]
    public string elementId { get; set; }
  }

  public class RoomBoundaryLine : Base, IBaseRevitElement
  {
    public ICurve baseCurve { get; set; }

    [SchemaOptional]
    public Dictionary<string, object> parameters { get; set; }

    [SchemaIgnore]
    public string elementId { get; set; }

  }
}
