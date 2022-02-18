using Objects.Geometry;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements
{
  public class Opening : Base
  {
    public ICurve outline { get; set; }

    public string units { get; set; }

    public Opening() { }

    //[SchemaInfo("Opening", "Creates a Speckle opening")]
    public Opening(ICurve outline)
    {
      this.outline = outline;
    }
  }
}

namespace Objects.BuiltElements.Revit
{
  public class RevitOpening : Opening
  {
    //public string family { get; set; }
    //public string type { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }

    public RevitOpening() { }
  }

  public class RevitVerticalOpening : RevitOpening
  {
  }

  public class RevitWallOpening : RevitOpening
  {
    public RevitWall host { get; set; }

    public RevitWallOpening() { }
  }

  public class RevitShaft : RevitOpening
  {
    public Level bottomLevel { get; set; }
    public Level topLevel { get; set; }
    public double height { get; set; }

    public RevitShaft() { }

    /// <summary>
    /// SchemaBuilder constructor for a Revit shaft
    /// </summary>
    /// <param name="outline"></param>
    /// <param name="bottomLevel"></param>
    /// <param name="topLevel"></param>
    /// <param name="parameters"></param>
    [SchemaInfo("RevitShaft", "Creates a Revit shaft from a bottom and top level", "Revit", "Architecture")]
    public RevitShaft([SchemaMainParam] ICurve outline, Level bottomLevel, Level topLevel, List<Parameter> parameters = null)
    {
      this.outline = outline;
      this.bottomLevel = bottomLevel;
      this.topLevel = topLevel;
      this.parameters = parameters.ToBase();
    }

    /*
    /// <summary>
    /// SchemaBuilder constructor for a Revit shaft
    /// </summary>
    /// <param name="outline"></param>
    /// <param name="bottomLevel"></param>
    /// <param name="height"></param>
    /// <param name="parameters"></param>
    /// <remarks>Assign units when using this constructor due to <paramref name="height"/> param</remarks>
    [SchemaInfo("RevitShaft", "Creates a Revit shaft from a bottom level and height")]
    public RevitShaft(ICurve outline, Level bottomLevel, double height, List<Parameter> parameters = null)
    {
      this.outline = outline;
      this.bottomLevel = bottomLevel;
      this.height = height;
      this.parameters = parameters.ToBase();
    }
    */
  }
}

namespace Objects.BuiltElements.TeklaStructures
{
    public class TeklaOpening : Opening
    {
        public string openingHostId { get; set; }
        public TeklaOpeningTypeEnum openingType { get; set; }
        public TeklaOpening() { }
    }
    public class TeklaContourOpening : TeklaOpening
    {
        public TeklaContourPlate cuttingPlate { get; set; }
        public double thickness { get; set; }
        public TeklaContourOpening() { }
    }
    public class TeklaBeamOpening : TeklaOpening
    {
        public TeklaBeam cuttingBeam { get; set; }
        public TeklaBeamOpening() { }
    }
}
