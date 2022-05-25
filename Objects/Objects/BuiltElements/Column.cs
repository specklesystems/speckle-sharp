using Objects.Geometry;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Objects.BuiltElements
{
  public class Column : Base, IDisplayMesh, IDisplayValue<List<Mesh>>
  {
    public ICurve baseLine { get; set; }

    [DetachProperty]
    public List<Mesh> displayValue { get; set; }

    public string units { get; set; }

    public Column() { }

    [SchemaInfo("Column", "Creates a Speckle column", "BIM", "Structure")]
    public Column([SchemaMainParam] ICurve baseLine)
    {
      this.baseLine = baseLine;
    }

    #region Obsolete Members
    [JsonIgnore, Obsolete("Use " + nameof(displayValue) + " instead")]
    public Mesh displayMesh
    {
      get => displayValue?.FirstOrDefault();
      set => displayValue = new List<Mesh> { value };
    }
    #endregion
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
    //public bool structural { get; set; }
    public double rotation { get; set; }
    public bool isSlanted { get; set; }
    public string family { get; set; }
    public string type { get; set; }
    public Base parameters { get; set; }
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
    [SchemaInfo("RevitColumn Vertical", "Creates a vertical Revit Column by point and levels.", "Revit", "Architecture")]
    public RevitColumn(string family, string type,
      [SchemaParamInfo("Only the lower point of this line will be used as base point.")][SchemaMainParam] ICurve baseLine,
      Level level, Level topLevel,
      double baseOffset = 0, double topOffset = 0, bool structural = false,
      [SchemaParamInfo("Rotation angle in radians")] double rotation = 0, List<Parameter> parameters = null)
    {
      this.family = family;
      this.type = type;
      this.baseLine = baseLine;
      this.topLevel = topLevel;
      this.baseOffset = baseOffset;
      this.topOffset = topOffset;
      //this.structural = structural;
      this.rotation = rotation;
      this.parameters = parameters.ToBase();
      this.level = level;
    }

    [SchemaDeprecated]
    [SchemaInfo("RevitColumn Slanted (old)", "Creates a slanted Revit Column by curve.", "Revit", "Structure")]
    public RevitColumn(string family, string type, [SchemaMainParam] ICurve baseLine, Level level, bool structural = false, List<Parameter> parameters = null)
    {
      this.family = family;
      this.type = type;
      this.baseLine = baseLine;
      this.level = level;
      //this.structural = structural;
      this.isSlanted = true;
      this.parameters = parameters.ToBase();
    }

    [SchemaInfo("RevitColumn Slanted", "Creates a slanted Revit Column by curve.", "Revit", "Structure")]
    public RevitColumn(string family, string type, [SchemaMainParam] ICurve baseLine, Level level, Level topLevel = null, bool structural = false, List<Parameter> parameters = null)
    {
      this.family = family;
      this.type = type;
      this.baseLine = baseLine;
      this.level = level;
      this.topLevel = topLevel;
      //this.structural = structural;
      this.isSlanted = true;
      this.parameters = parameters.ToBase();
    }
  }
}
