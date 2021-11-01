using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Structural.Geometry;
using Objects.Structural.Materials;
using Objects.Structural.Properties.Profiles;

namespace Objects.Structural.AdSec.Properties.Profiles
{
  public class AdSecCruciform : SectionProfile
  {
    public double depth { get; set; }
    public double width { get; set; }
    public double webThickness { get; set; }
    public double flangeThickness { get; set; }

    public AdSecCruciform() { }

    [SchemaInfo("Cruciform", "Creates a Speckle structural cruciform section profile", "AdSec", "Section Profile")]
    public AdSecCruciform(string name, double depth, double width, double webThickness, double flangeThickness)
    {
      this.depth = depth;
      this.width = width;
      this.webThickness = webThickness;
      this.flangeThickness = flangeThickness;
    }
  }

  public class AdSecEllipse : SectionProfile
  {
    public double depth { get; set; }
    public double width { get; set; }
    public double wallThickness { get; set; }

    public AdSecEllipse() { }

    [SchemaInfo("Ellipse", "Creates a Speckle structural ellipse section profile", "AdSec", "Section Profile")]
    public AdSecEllipse(string name, double depth, double width, double wallThickness = 0)
    {
      this.depth = depth;
      this.width = width;
      this.wallThickness = wallThickness;
    }
  }

  public class AdSecCSection : SectionProfile
  {
    public double depth { get; set; }
    public double width { get; set; }
    public double wallThickness { get; set; }
    public double lipDepth { get; set; }

    public AdSecCSection() { }

    [SchemaInfo("General C", "Creates a Speckle structural C section profile", "AdSec", "Section Profile")]
    public AdSecCSection(string name, double depth, double width, double wallThickness, double lipDepth)
    {
      this.depth = depth;
      this.width = width;
      this.wallThickness = wallThickness;
      this.lipDepth = lipDepth;
    }
  }

  public class AdSecZSection : SectionProfile
  {
    public double depth { get; set; }
    public double topFlangeWidth { get; set; }
    public double botFlangeWidth { get; set; }
    public double wallThickness { get; set; }
    public double topLipDepth { get; set; }
    public double botLipDepth { get; set; }

    public AdSecZSection() { }

    [SchemaInfo("General Z", "Creates a Speckle structural Z section profile", "AdSec", "Section Profile")]
    public AdSecZSection(string name, double depth, double topFlangeWidth, double botFlangeWidth, double wallThickness, double topLipDepth, double botLipDepth)
    {
      this.depth = depth;
      this.topFlangeWidth = topFlangeWidth;
      this.botFlangeWidth = botFlangeWidth;
      this.wallThickness = wallThickness;
      this.topLipDepth = topLipDepth;
      this.botLipDepth = botLipDepth;
    }
  }

  public class AdSecIAssymetric : SectionProfile
  {
    public double depth { get; set; }
    public double topFlangeWidth { get; set; }
    public double botFlangeWidth { get; set; }
    public double wallThickness { get; set; }
    public double topFlangeThickness { get; set; }
    public double botFlangeThickness { get; set; }

    public AdSecIAssymetric() { }

    [SchemaInfo("Assymetric I", "Creates a Speckle structural assymetric I section profile", "AdSec", "Section Profile")]
    public AdSecIAssymetric(string name, double depth, double topFlangeWidth, double botFlangeWidth, double wallThickness, double topFlangeThickness, double botFlangeThickness)
    {
      this.depth = depth;
      this.topFlangeWidth = topFlangeWidth;
      this.botFlangeWidth = botFlangeWidth;
      this.wallThickness = wallThickness;
      this.topFlangeThickness = topFlangeThickness;
      this.botFlangeThickness = botFlangeThickness;
    }
  }

  public class AdSecRectoEllipse : SectionProfile
  {
    public double depth { get; set; }
    public double width { get; set; }
    public double depthFlat { get; set; }
    public double widthFlat { get; set; }

    public AdSecRectoEllipse() { }

    [SchemaInfo("Recto Ellipse", "Creates a Speckle structural rectangular profile with elliptical corners", "AdSec", "Section Profile")]
    public AdSecRectoEllipse(string name, double depth, double width, double depthFlat, double widthFlat)
    {
      this.depth = depth;
      this.width = width;
      this.depthFlat = depthFlat;
      this.widthFlat = widthFlat;
    }
  }

  public class AdSecSecantPile : SectionProfile
  {
    public double diameter { get; set; }
    public double pileCentres { get; set; }
    public int pileCount { get; set; }
    public bool isWall { get; set; }

    public AdSecSecantPile() { }

    [SchemaInfo("Secant Pile", "Creates a Speckle structural secant pile section profile", "AdSec", "Section Profile")]
    public AdSecSecantPile(string name, double diameter, double pileCentres, int pileCount, bool isWall)
    {
      this.diameter = diameter;
      this.pileCentres = pileCentres;
      this.pileCount = pileCount;
      this.isWall = isWall;
    }
  }

  public class AdSecSheetPile : SectionProfile
  {
    public double depth { get; set; }
    public double width { get; set; }
    public double topFlangeWidth { get; set; }
    public double botFlangeWidth { get; set; }
    public double webThickness { get; set; }
    public double flangeThickness { get; set; }

    public AdSecSheetPile() { }

    [SchemaInfo("Sheet Pile", "Creates a Speckle structural sheet pile section profile", "AdSec", "Section Profile")]
    public AdSecSheetPile(string name, double depth, double width, double topFlangeWidth, double botFlangeWidth, double webThickness, double flangeThickness)
    {
      this.depth = depth;
      this.width = width;
      this.topFlangeWidth = topFlangeWidth;
      this.botFlangeWidth = botFlangeWidth;
      this.webThickness = webThickness;
      this.flangeThickness = flangeThickness;
    }
  }

  public class AdSecStadium : SectionProfile
  {
    public double depth { get; set; }
    public double width { get; set; }

    public AdSecStadium() { }

    [SchemaInfo("Stadium", "Creates a Speckle structural stadium section profile. It is a profile consisting of a rectangle whose ends are capped off with semicircles", "AdSec", "Section Profile")]
    public AdSecStadium(string name, double depth, double width)
    {
      this.depth = depth;
      this.width = width;
    }
  }

  public class AdSecTrapezoid : SectionProfile
  {
    public double depth { get; set; }
    public double topWidth { get; set; }
    public double botWidth { get; set; }

    public AdSecTrapezoid() { }

    [SchemaInfo("Trapezoid", "Creates a Speckle structural trapezoidal section profile", "AdSec", "Section Profile")]
    public AdSecTrapezoid(string name, double depth, double topWidth, double botWidth)
    {
      this.depth = depth;
      this.topWidth = topWidth;
      this.botWidth = botWidth;
    }
  }
}
