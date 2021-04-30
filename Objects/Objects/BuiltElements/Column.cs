using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements
{
  public class Column : Base, IDisplayMesh
  {
    public ICurve baseLine { get; set; }

    [DetachProperty]
    public Mesh displayMesh { get; set; }

    public Column() { }

    [SchemaInfo("Column", "Creates a Speckle column")]
    public Column([SchemaMainParam] ICurve baseLine)
    {
      this.baseLine = baseLine;
    }
  }
}

namespace Objects.BuiltElements.Revit
{
  public class RevitColumn : Column
  {
    public Level level { get; set; }
    public Level topLevel { get; set; }
    public double baseOffset { get; set; }
    public double topOffset { get; set; }
    public bool facingFlipped { get; set; }
    public bool handFlipped { get; set; }
    public bool structural { get; set; }
    public double rotation { get; set; }
    public bool isSlanted { get; set; }
    public string family { get; set; }
    public string type { get; set; }
    public List<Parameter> parameters { get; set; }
    public string elementId { get; set; }

    public RevitColumn() { }

    /// <summary>
    /// SchemaBuilder constructor for a Revit column
    /// </summary>
    /// <param name="family"></param>
    /// <param name="type"></param>
    /// <param name="baseLine"></param>
    /// <param name="level"></param>
    /// <param name="topLevel"></param>
    /// <param name="baseOffset"></param>
    /// <param name="topOffset"></param>
    /// <param name="structural"></param>
    /// <param name="rotation"></param>
    /// <param name="parameters"></param>
    /// <remarks>Assign units when using this constructor due to <paramref name="baseOffset"/> and <paramref name="topOffset"/> params</remarks>
    [SchemaInfo("Vertical Column", "Creates a vertical Revit Column by point and levels.")]
    public RevitColumn(string family, string type,
      [SchemaParamInfo("Only the lower point of this line will be used as base point.")][SchemaMainParam] ICurve baseLine,
      Level level, Level topLevel,
      double baseOffset = 0, double topOffset = 0, bool structural = false,
      double rotation = 0, List<Parameter> parameters = null)
    {
      this.family = family;
      this.type = type;
      this.baseLine = baseLine;
      this.topLevel = topLevel;
      this.baseOffset = baseOffset;
      this.topOffset = topOffset;
      this.structural = structural;
      this.rotation = rotation;
      this.parameters = parameters;
      this.level = level;
    }

    [SchemaInfo("Slanted Column", "Creates a slanted Revit Column by curve.")]
    public RevitColumn(string family, string type, [SchemaMainParam] ICurve baseLine, Level level, bool structural = false, List<Parameter> parameters = null)
    {
      this.family = family;
      this.type = type;
      this.baseLine = baseLine;
      this.level = level;
      this.structural = structural;
      this.parameters = parameters;
    }
  }
}
