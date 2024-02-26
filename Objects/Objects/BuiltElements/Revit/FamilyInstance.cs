using System.Collections.Generic;
using Objects.Geometry;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Revit;

public class FamilyInstance : Base, IDisplayValue<List<Mesh>>
{
  public FamilyInstance() { }

  [SchemaInfo("FamilyInstance", "Creates a Revit family instance", "Revit", "Families")]
  public FamilyInstance(
    Point basePoint,
    string family,
    string type,
    Level level,
    double rotation = 0,
    bool facingFlipped = false,
    bool handFlipped = false,
    List<Parameter>? parameters = null
  )
  {
    this.basePoint = basePoint;
    this.family = family;
    this.type = type;
    this.level = level;
    this.rotation = rotation;
    this.facingFlipped = facingFlipped;
    this.handFlipped = handFlipped;
    mirrored = false;
    this.parameters = parameters?.ToBase();
  }

  public Point basePoint { get; set; }
  public string family { get; set; }
  public string type { get; set; }
  public string category { get; set; }
  public Level level { get; set; }
  public double rotation { get; set; }
  public bool facingFlipped { get; set; }
  public bool handFlipped { get; set; }
  public bool mirrored { get; set; }
  public Base? parameters { get; set; }
  public string elementId { get; set; }

  [DetachProperty]
  public List<Base> elements { get; set; }

  public string units { get; set; }

  [DetachProperty]
  public List<Mesh> displayValue { get; set; }
}
