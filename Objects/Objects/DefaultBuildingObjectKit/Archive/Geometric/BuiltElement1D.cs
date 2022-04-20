using Objects.Geometry;
using Objects.Structural.Materials;
using Objects.Structural.Properties.Profiles;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements
{
  public class BuiltElement1D : Base, IDisplayMesh, IDisplayValue<List<Mesh>>
  {
    public ICurve baseLine { get; set; }

    [DetachProperty]
    public SectionProfile profile { get; set; }

    [DetachProperty]
    public Material material { get; set; }

    [DetachProperty]
    public List<Mesh> displayValue { get; set; }

    public string units { get; set; }

    public Element1DType ElementType { get; set; }
    public BuiltElement1D() { }

    [SchemaInfo("BuiltElement1D", "Creates a Speckle 1D Element", "BIM", "Structure")]
    public BuiltElement1D([SchemaMainParam] ICurve baseLine, Element1DType element1DType, Material material = null, SectionProfile profile = null)
    {
      this.baseLine = baseLine;
      this.ElementType = element1DType;
      this.material = material;
      this.profile = profile;
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
  public class RevitBeam : BuiltElement1D
  {
    public string family { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }
    public Level level { get; set; }

    public RevitBeam() { }

    [SchemaInfo("RevitBeam", "Creates a Revit beam by curve and base level.", "Revit", "Structure")]
    public RevitBeam(string family, string type, [SchemaMainParam] ICurve baseLine, Level level, List<Parameter> parameters = null)
    {
      this.family = family;
      this.baseLine = baseLine;
      this.parameters = parameters.ToBase();
      this.level = level;
      this.ElementType = Element1DType.Beam;
      this.profile.name = type;
    }
  }
}

namespace Objects.BuiltElements.Revit
{
  public class RevitColumn : BuiltElement1D
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
    public Base parameters { get; set; }
    public string elementId { get; set; }

    public RevitColumn() { }

    /// <summary>
    /// SchemaBuilder constructor for a Revit column
    /// </summary>
    /// <param name="family"></param>
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
      double rotation = 0, List<Parameter> parameters = null)
    {
      this.family = family;
      this.baseLine = baseLine;
      this.topLevel = topLevel;
      this.baseOffset = baseOffset;
      this.topOffset = topOffset;
      //this.structural = structural;
      this.rotation = rotation;
      this.parameters = parameters.ToBase();
      this.level = level;
      this.ElementType = Element1DType.Column;
      this.profile.name = type;
    }

    [SchemaInfo("RevitColumn Slanted", "Creates a slanted Revit Column by curve.", "Revit", "Structure")]
    public RevitColumn(string family, string type, [SchemaMainParam] ICurve baseLine, Level level, bool structural = false, List<Parameter> parameters = null)
    {
      this.family = family;
      this.baseLine = baseLine;
      this.level = level;
      //this.structural = structural;
      this.isSlanted = true;
      this.parameters = parameters.ToBase();
      this.ElementType = Element1DType.Column;
      this.profile.name = type;
    }
  }
}


namespace Objects.BuiltElements.Revit
{
  public class RevitBrace : BuiltElement1D
  {
    public string family { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }
    public Level level { get; set; }

    public RevitBrace() { }

    [SchemaInfo("RevitBrace", "Creates a Revit brace by curve and base level.", "Revit", "Structure")]
    public RevitBrace(string family, string type, [SchemaMainParam] ICurve baseLine, Level level, List<Parameter> parameters = null)
    {
      this.family = family;
      this.baseLine = baseLine;
      this.parameters = parameters.ToBase();
      this.level = level;
      this.ElementType = Element1DType.Brace;
      this.profile.name = type;
    }
  }
}
namespace Objects.BuiltElements.TeklaStructures
{
  public class TeklaBeam : BuiltElement1D
  {
    public string name { get; set; }
    public string finish { get; set; }
    public string classNumber { get; set; }
    public Vector alignmentVector { get; set; } // This can be set to get proper rotation if coming from an application that doesn't have positioning
    public TeklaPosition position { get; set; }
    public Base userProperties { get; set; }

    [DetachProperty]
    public Base rebars { get; set; }

    public TeklaBeamType TeklaBeamType { get; set; }

    public TeklaBeam() { }

    [SchemaInfo("TeklaBeam", "Creates a Tekla Structures beam by curve.", "Tekla", "Structure")]
    public TeklaBeam([SchemaMainParam] ICurve baseLine, SectionProfile profile, Material material)
    {
      this.baseLine = baseLine;
      this.profile = profile;
      this.material = material;
    }
  }
  public class SpiralBeam : TeklaBeam
  {
    public SpiralBeam()
    {
    }

    public Point startPoint { get; set; }
    public Point rotationAxisPt1 { get; set; }
    public Point rotationAxisPt2 { get; set; }
    public double totalRise { get; set; }
    public double rotationAngle { get; set; }
    public double twistAngleStart { get; set; }
    public double twistAngleEnd { get; set; }


  }
}

