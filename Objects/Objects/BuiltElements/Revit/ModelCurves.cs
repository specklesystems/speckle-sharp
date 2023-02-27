using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.BuiltElements.Revit.Curve
{
  public class ModelCurve : Base
  {
    public ICurve baseCurve { get; set; }
    public string lineStyle { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }

    public string units { get; set; }

    public ModelCurve() { }

    [SchemaInfo("ModelCurve", "Creates a Revit model curve", "Revit", "Curves")]
    public ModelCurve([SchemaMainParam] ICurve baseCurve, string lineStyle, List<Parameter> parameters = null)
    {
      this.baseCurve = baseCurve;
      this.lineStyle = lineStyle;
      this.parameters = parameters.ToBase();
    }
  }

  public class DetailCurve : Base
  {
    public ICurve baseCurve { get; set; }
    public string lineStyle { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }

    public string units { get; set; }

    public DetailCurve() { }

    [SchemaInfo("DetailCurve", "Creates a Revit detail curve", "Revit", "Curves")]
    public DetailCurve([SchemaMainParam] ICurve baseCurve, string lineStyle, List<Parameter> parameters = null)
    {
      this.baseCurve = baseCurve;
      this.lineStyle = lineStyle;
      this.parameters = parameters.ToBase();
    }
  }

  public class RoomBoundaryLine : Base
  {
    public ICurve baseCurve { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }
    public Level level { get; set; }
    public string units { get; set; }

    public RoomBoundaryLine() { }

    [SchemaInfo("RoomBoundaryLine", "Creates a Revit room boundary line", "Revit", "Curves")]
    public RoomBoundaryLine([SchemaMainParam] ICurve baseCurve, List<Parameter> parameters = null)
    {
      this.baseCurve = baseCurve;
      this.parameters = parameters.ToBase();
    }
  }

  public class SpaceSeparationLine : Base
  {
    public ICurve baseCurve { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }
    public string units { get; set; }
    public SpaceSeparationLine() { }

    [SchemaInfo("SpaceSeparationLine", "Creates a Revit space separation line", "Revit", "Curves")]
    public SpaceSeparationLine([SchemaMainParam] ICurve baseCurve, List<Parameter> parameters = null)
    {
      this.baseCurve = baseCurve;
      this.parameters = parameters.ToBase();
    }
  }
}
