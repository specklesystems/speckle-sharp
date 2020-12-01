using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.BuiltElements.Revit
{
  public class ModelCurve : Base
  {
    public ICurve baseCurve { get; set; }
    public string lineStyle { get; set; }

    [SchemaOptional]
    public Dictionary<string, object> parameters { get; set; }

    [SchemaIgnore]
    public string elementId { get; set; }
  }

  public class DetailCurve : Base
  {
    public ICurve baseCurve { get; set; }
    public string lineStyle { get; set; }

    [SchemaOptional]
    public Dictionary<string, object> parameters { get; set; }

    [SchemaIgnore]
    public string elementId { get; set; }
  }

  public class RoomBoundaryLine : Base
  {
    public ICurve baseCurve { get; set; }

    [SchemaOptional]
    public Dictionary<string, object> parameters { get; set; }

    [SchemaIgnore]
    public string elementId { get; set; }
  }
}
