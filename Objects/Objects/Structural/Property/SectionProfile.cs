using System.Collections.Generic;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.Properties.Profiles;

public class SectionProfile : Base //section profile description
{
  public SectionProfile() { }

  public SectionProfile(
    string name,
    ShapeType shapeType,
    double area,
    double Iyy,
    double Izz,
    double J,
    double Ky,
    double Kz,
    double weight
  )
  {
    this.name = name;
    this.shapeType = shapeType;
    this.area = area;
    this.Iyy = Izz;
    this.Izz = Izz;
    this.J = J;
    this.Ky = Ky;
    this.Kz = Kz;
    this.weight = weight;
  }

  public string name { get; set; }
  public virtual ShapeType shapeType { get; set; } = ShapeType.Undefined;

  public string shapeName => shapeType.ToString();

  public double area { get; set; }

  //Moment of inertia about the Major axis
  public double Iyy { get; set; }
  public double Izz { get; set; }
  public double J { get; set; }
  public double Ky { get; set; }
  public double Kz { get; set; }
  public double weight { get; set; } //section weight, ex. kg/m weight is defined in materials though more ? rather than section profile.
  public string units { get; set; }
}

public class Rectangular : SectionProfile
{
  public Rectangular() { }

  [SchemaInfo(
    "Rectangular",
    "Creates a Speckle structural rectangular section profile",
    "Structural",
    "Section Profile"
  )]
  public Rectangular(string name, double depth, double width, double webThickness = 0, double flangeThickness = 0)
  {
    this.name = name;
    this.depth = depth;
    this.width = width;
    this.webThickness = webThickness;
    this.flangeThickness = flangeThickness;
  }

  public double depth { get; set; }
  public double width { get; set; }
  public double webThickness { get; set; } // tw
  public double flangeThickness { get; set; } // tf
  public override ShapeType shapeType { get; set; } = ShapeType.Rectangular;
}

public class Circular : SectionProfile
{
  public Circular() { }

  [SchemaInfo("Circular", "Creates a Speckle structural circular section profile", "Structural", "Section Profile")]
  public Circular(string name, double radius, double wallThickness = 0)
  {
    this.name = name;
    this.radius = radius;
    this.wallThickness = wallThickness;
  }

  public double radius { get; set; }
  public double wallThickness { get; set; }
  public override ShapeType shapeType { get; set; } = ShapeType.Circular;
}

public class ISection : SectionProfile
{
  public ISection() { }

  [SchemaInfo("ISection", "Creates a Speckle structural I section profile", "Structural", "Section Profile")]
  public ISection(string name, double depth, double width, double webThickness, double flangeThickness)
  {
    this.name = name;
    this.depth = depth;
    this.width = width;
    this.webThickness = webThickness;
    this.flangeThickness = flangeThickness;
  }

  public double depth { get; set; }
  public double width { get; set; }
  public double webThickness { get; set; }
  public double flangeThickness { get; set; }
  public override ShapeType shapeType { get; set; } = ShapeType.I;
}

public class Tee : SectionProfile
{
  public Tee() { }

  [SchemaInfo("Tee", "Creates a Speckle structural Tee section profile", "Structural", "Section Profile")]
  public Tee(string name, double depth, double width, double webThickness, double flangeThickness)
  {
    this.name = name;
    this.depth = depth;
    this.width = width;
    this.webThickness = webThickness;
    this.flangeThickness = flangeThickness;
  }

  public double depth { get; set; }
  public double width { get; set; }
  public double webThickness { get; set; }
  public double flangeThickness { get; set; }
  public override ShapeType shapeType { get; set; } = ShapeType.Tee;
}

public class Angle : SectionProfile
{
  public Angle() { }

  [SchemaInfo("Angle", "Creates a Speckle structural angle section profile", "Structural", "Section Profile")]
  public Angle(string name, double depth, double width, double webThickness, double flangeThickness)
  {
    this.name = name;
    this.depth = depth;
    this.width = width;
    this.webThickness = webThickness;
    this.flangeThickness = flangeThickness;
  }

  public double depth { get; set; }
  public double width { get; set; }
  public double webThickness { get; set; }
  public double flangeThickness { get; set; }
  public override ShapeType shapeType { get; set; } = ShapeType.Angle;
}

public class Channel : SectionProfile
{
  public Channel() { }

  [SchemaInfo("Channel", "Creates a Speckle structural channel section profile", "Structural", "Section Profile")]
  public Channel(string name, double depth, double width, double webThickness, double flangeThickness)
  {
    this.name = name;
    this.depth = depth;
    this.width = width;
    this.webThickness = webThickness;
    this.flangeThickness = flangeThickness;
  }

  public double depth { get; set; }
  public double width { get; set; }
  public double webThickness { get; set; }
  public double flangeThickness { get; set; }
  public override ShapeType shapeType { get; set; } = ShapeType.Channel;
}

public class Perimeter : SectionProfile
{
  public Perimeter() { }

  [SchemaInfo(
    "Perimeter",
    "Creates a Speckle structural section profile defined by a perimeter curve and, if applicable, a list of void curves",
    "Structural",
    "Section Profile"
  )]
  public Perimeter(string name, ICurve outline, List<ICurve> voids = null)
  {
    this.name = name;
    this.outline = outline;
    this.voids = voids;
  }

  public ICurve outline { get; set; }
  public List<ICurve> voids { get; set; } = new();
}

public class Catalogue : SectionProfile
{
  public Catalogue() { }

  [SchemaInfo(
    "Catalogue (by description)",
    "Creates a Speckle structural section profile based on a catalogue section description",
    "Structural",
    "Section Profile"
  )]
  public Catalogue(string description)
  {
    this.description = description;
  }

  [SchemaInfo("Catalogue", "Creates a Speckle structural section profile", "Structural", "Section Profile")]
  public Catalogue(string name, string catalogueName, string sectionType, string sectionName)
  {
    this.name = name;
    this.catalogueName = catalogueName;
    this.sectionType = sectionType;
    this.sectionName = sectionName;
  }

  public string description { get; set; } // a description string for a catalogue section, per a to be defined convention for industry-typical, commonly manufactured sections - SAF Formcodes, Oasys profiles?
  public string catalogueName { get; set; } // ex. AISC, could be enum value
  public string sectionType { get; set; } // ex. W shapes, could be enum value
  public string sectionName { get; set; } // ex. W44x335, could be enum value
}

public class Explicit : SectionProfile
{
  public Explicit() { }

  [SchemaInfo(
    "Explicit",
    "Creates a Speckle structural section profile based on explicitly defining geometric properties",
    "Structural",
    "Section Profile"
  )]
  public Explicit(string name, double area, double Iyy, double Izz, double J, double Ky, double Kz)
  {
    this.name = name;
    this.area = area;
    this.Iyy = Iyy;
    this.Izz = Izz;
    this.J = J;
    this.Ky = Ky;
    this.Kz = Kz;
  }

  public override ShapeType shapeType { get; set; } = ShapeType.Explicit;
}
