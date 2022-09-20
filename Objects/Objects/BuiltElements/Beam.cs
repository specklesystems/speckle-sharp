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
  public class Beam : Base, IDisplayMesh, IDisplayValue<List<Mesh>>
  {
    public ICurve baseLine { get; set; }

    [DetachProperty]
    public List<Mesh> displayValue { get; set; }

    public string units { get; set; }

    public Beam() { }

    [SchemaInfo("Beam", "Creates a Speckle beam", "BIM", "Structure")]
    public Beam([SchemaMainParam] ICurve baseLine)
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
  public class RevitBeam : Beam
  {
    public string family { get; set; }
    public string type { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }
    public Level level { get; set; }

    public RevitBeam() { }

    [SchemaInfo("RevitBeam", "Creates a Revit beam by curve and base level.", "Revit", "Structure")]
    public RevitBeam(string family, string type, [SchemaMainParam] ICurve baseLine, Level level, List<Parameter> parameters = null)
    {
      this.family = family;
      this.type = type;
      this.baseLine = baseLine;
      this.parameters = parameters.ToBase();
      this.level = level;
    }
  }
}

namespace Objects.BuiltElements.TeklaStructures
{
  public class TeklaBeam : Beam, IHasVolume, IHasArea
  {
    public string name { get; set; }
    [DetachProperty]
    public SectionProfile profile { get; set; }
    [DetachProperty]
    public StructuralMaterial material { get; set; }
        [DetachProperty]
        public string finish { get; set; }
        [DetachProperty]
        public string classNumber { get; set; }
    public Vector alignmentVector { get; set; } // This can be set to get proper rotation if coming from an application that doesn't have positioning
        [DetachProperty]
        public TeklaPosition position { get; set; }
    public Base userProperties { get; set; }

    [DetachProperty]
    public Base rebars { get; set; }

    public TeklaBeamType TeklaBeamType { get; set; }
    public double volume { get; set; }
    public double area { get; set ; }

    public TeklaBeam() { }

    [SchemaInfo("TeklaBeam", "Creates a Tekla Structures beam by curve.", "Tekla", "Structure")]
    public TeklaBeam([SchemaMainParam] ICurve baseLine, SectionProfile profile, StructuralMaterial material)
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
