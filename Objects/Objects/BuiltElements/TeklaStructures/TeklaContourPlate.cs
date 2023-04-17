using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Materials;
using Objects.Structural.Properties.Profiles;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements.TeklaStructures;

public class TeklaContourPlate : Area
{
  [SchemaInfo("ContourPlate", "Creates a TeklaStructures contour plate.", "Tekla", "Structure")]
  public TeklaContourPlate(
    SectionProfile profile,
    Polyline outline,
    string finish,
    string classNumber,
    string units,
    StructuralMaterial material = null,
    TeklaPosition position = null,
    Base rebars = null
  )
  {
    this.profile = profile;
    this.outline = outline;
    this.material = material;
    this.finish = finish;
    this.classNumber = classNumber;
    this.position = position;
    this.rebars = rebars;
    this.units = units;
  }

  public TeklaContourPlate() { }

  [DetachProperty]
  public SectionProfile profile { get; set; }

  [DetachProperty]
  public StructuralMaterial material { get; set; }

  [DetachProperty]
  public string finish { get; set; }

  [DetachProperty]
  public string classNumber { get; set; }

  [DetachProperty]
  public TeklaPosition position { get; set; }

  [DetachProperty]
  public Base rebars { get; set; }

  public List<TeklaContourPoint> contour { get; set; } // Use for ToNative to Tekla. Other programs can use Area.outline.
}

public class TeklaContourPoint : Point
{
  public TeklaContourPoint() { }

  public TeklaContourPoint(Point point) { }

  public TeklaChamferType chamferType { get; set; }
  public double xDim { get; set; }
  public double yDim { get; set; }
  public double dz1 { get; set; }
  public double dz2 { get; set; }
}
