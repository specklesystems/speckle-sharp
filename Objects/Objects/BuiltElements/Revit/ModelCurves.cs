using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.BuiltElements.Revit.Curve
{
  public class ModelCurve : Base
  {
    public ICurve baseCurve { get; set; }
    public string lineStyle { get; set; }
    public List<Parameter> parameters { get; set; }
    public string elementId { get; set; }

    public ModelCurve() { }

    [SchemaInfo("ModelCurve", "Creates a Revit model curve")]
    public ModelCurve(ICurve baseCurve, string lineStyle, List<Parameter> parameters = null)
    {
      this.baseCurve = baseCurve;
      this.lineStyle = lineStyle;
      this.parameters = parameters;
    }
  }

  public class DetailCurve : Base
  {
    public ICurve baseCurve { get; set; }
    public string lineStyle { get; set; }
    public List<Parameter> parameters { get; set; }
    public string elementId { get; set; }

    public DetailCurve() { }

    [SchemaInfo("DetailCurve", "Creates a Revit detail curve")]
    public DetailCurve(ICurve baseCurve, string lineStyle, List<Parameter> parameters = null)
    {
      this.baseCurve = baseCurve;
      this.lineStyle = lineStyle;
      this.parameters = parameters;
    }
  }

  public class RoomBoundaryLine : Base
  {
    public ICurve baseCurve { get; set; }
    public List<Parameter> parameters { get; set; }
    public string elementId { get; set; }

    public RoomBoundaryLine() { }

    [SchemaInfo("RoomBoundaryLine", "Creates a Revit room boundary line")]
    public RoomBoundaryLine(ICurve baseCurve, List<Parameter> parameters = null)
    {
      this.baseCurve = baseCurve;
      this.parameters = parameters;
    }
  }
}
