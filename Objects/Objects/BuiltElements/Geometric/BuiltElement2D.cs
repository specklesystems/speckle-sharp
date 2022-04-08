using Objects.Geometry;
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
  public class BuiltElement2D : Base, IDisplayMesh, IDisplayValue<List<Mesh>>
  {
  //SHOULD WALL BE HERE ? 
    public ICurve outline { get; set; }
    public List<ICurve> voids { get; set; } = new List<ICurve>();

    [DetachProperty]
    public List<Mesh> displayValue { get; set; }

    [DetachProperty]
    public List<Base> elements { get; set; }

    //To do add:Section Property 2D here from structural objects ?; 

    public string units { get; set; }

    public Element2DType element2DType { get; set; }

    public BuiltElement2D() { }

    [SchemaInfo("BuiltElement2D", "Creates a Speckle BuiltElement2D", "BIM", "Architecture")]
    public BuiltElement2D([SchemaMainParam] ICurve outline, Element2DType element2DType,List<ICurve> voids = null,
      [SchemaParamInfo("Any nested elements that this BuiltElement2D might have")] List<Base> elements = null)
    {
      this.outline = outline;
      this.voids = voids;
      this.element2DType = element2DType;
      this.elements = elements;
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
  public class RevitFloor : BuiltElement2D
  {
    public string family { get; set; }
    public string type { get; set; }
    public Level level { get; set; }
    public bool structural { get; set; }
    public double slope { get; set; }
    public Line slopeDirection { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }
    public RevitFloor() { }

    [SchemaInfo("RevitFloor", "Creates a Revit floor by outline and level", "Revit", "Architecture")]
    public RevitFloor(string family, string type, [SchemaMainParam] ICurve outline,
       Level level, bool structural = false, double slope = 0, Line slopeDirection = null, List<ICurve> voids = null,
      [SchemaParamInfo("Any nested elements that this floor might have")] List<Base> elements = null,
      List<Parameter> parameters = null)
    {
      this.family = family;
      this.type = type;
      this.level = level;
      this.structural = structural;
      this.slope = slope;
      this.slopeDirection = slopeDirection;
      this.parameters = parameters.ToBase();
      this.outline = outline;
      this.voids = voids;
      this.elements = elements;
      this.element2DType = Element2DType.Floor;
    }
  }
}

namespace Objects.BuiltElements.Revit.RevitRoof
{
  public class RevitRoof : BuiltElement2D
  {
    public string family { get; set; }
    public string type { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }
    public Level level { get; set; }

    public RevitRoof() { }
  }

  public class RevitExtrusionRoof : RevitRoof
  {
    public double start { get; set; }
    public double end { get; set; }
    public Line referenceLine { get; set; }

    public RevitExtrusionRoof() { }

    /// <summary>
    /// SchemaBuilder constructor for a Revit extrusion roof
    /// </summary>
    /// <param name="family"></param>
    /// <param name="type"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="referenceLine"></param>
    /// <param name="level"></param>
    /// <param name="elements"></param>
    /// <param name="parameters"></param>
    /// <remarks>Assign units when using this constructor due to <paramref name="start"/> and <paramref name="end"/> params</remarks>
    [SchemaInfo("RevitExtrusionRoof", "Creates a Revit roof by extruding a curve", "Revit", "Architecture")]
    public RevitExtrusionRoof(string family, string type,
      [SchemaParamInfo("Extrusion start")] double start,
      [SchemaParamInfo("Extrusion end")] double end,
      [SchemaParamInfo("Profile along which to extrude the roof"), SchemaMainParam] Line referenceLine,
      Level level,
      List<Base> elements = null,
      List<Parameter> parameters = null)
    {
      this.family = family;
      this.type = type;
      this.parameters = parameters.ToBase();
      this.level = level;
      this.start = start;
      this.end = end;
      this.referenceLine = referenceLine;
      this.elements = elements;
      this.element2DType = Element2DType.Roof;
    }
  }

  public class RevitFootprintRoof : RevitRoof
  {
    public RevitLevel cutOffLevel { get; set; }
    public double? slope { get; set; }

    public RevitFootprintRoof() { }

    [SchemaInfo("RevitFootprintRoof", "Creates a Revit roof by outline", "Revit", "Architecture")]
    public RevitFootprintRoof([SchemaMainParam] ICurve outline, string family, string type, Level level, RevitLevel cutOffLevel = null, double slope = 0, List<ICurve> voids = null,
      List<Base> elements = null,
      List<Parameter> parameters = null)
    {
      this.outline = outline;
      this.voids = voids;
      this.family = family;
      this.type = type;
      this.slope = slope;
      this.parameters = parameters.ToBase();
      this.level = level;
      this.cutOffLevel = cutOffLevel;
      this.elements = elements;
      this.element2DType = Element2DType.Roof;
    }
  }
}

namespace Objects.BuiltElements.Revit
{
  public class RevitCeiling : BuiltElement2D
  {
    public string family { get; set; }
    public string type { get; set; }
    public Level level { get; set; }
    public double slope { get; set; }
    public Line slopeDirection { get; set; }
    public double offset { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }

    public RevitCeiling() { }

    [SchemaInfo("RevitCeiling", "Creates a Revit ceiling", "Revit", "Architecture")]
    public RevitCeiling([SchemaMainParam][SchemaParamInfo("Planar boundary curve")] ICurve outline, string family, string type, Level level,
      double slope = 0, [SchemaParamInfo("Planar line indicating slope direction")] Line slopeDirection = null, double offset = 0,
      List<ICurve> voids = null, [SchemaParamInfo("Any nested elements that this ceiling might have")] List<Base> elements = null)
    {
      this.outline = outline;
      this.family = family;
      this.type = type;
      this.level = level;
      this.slope = slope;
      this.slopeDirection = slopeDirection;
      this.offset = offset;
      this.voids = voids;
      this.elements = elements;
      this.element2DType = Element2DType.Ceiling;
    }
  }
}

namespace Objects.BuiltElements.Revit
{
  public class RevitWall : BuiltElement2D
  {
    public string family { get; set; }
    public string type { get; set; }
    public double baseOffset { get; set; }
    public double topOffset { get; set; }
    public bool flipped { get; set; }
    public bool structural { get; set; }
    public Level level { get; set; }
    public Level topLevel { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }

    public double height { get; set; }

    public RevitWall() { }

    /// <summary>
    /// SchemaBuilder constructor for a Revit wall
    /// </summary>
    /// <param name="family"></param>
    /// <param name="type"></param>
    /// <param name="baseLine"></param>
    /// <param name="level"></param>
    /// <param name="topLevel"></param>
    /// <param name="baseOffset"></param>
    /// <param name="topOffset"></param>
    /// <param name="flipped"></param>
    /// <param name="structural"></param>
    /// <param name="elements"></param>
    /// <param name="parameters"></param>
    /// <remarks>Assign units when using this constructor due to <paramref name="baseOffset"/> and <paramref name="topOffset"/> params</remarks>
    [SchemaInfo("RevitWall by curve and levels", "Creates a Revit wall with a top and base level.", "Revit", "Architecture")]
    public RevitWall(string family, string type,
      [SchemaMainParam] ICurve baseLine, Level level, Level topLevel, double baseOffset = 0, double topOffset = 0, bool flipped = false, bool structural = false,
      [SchemaParamInfo("Set in here any nested elements that this level might have.")] List<Base> elements = null,
      List<Parameter> parameters = null)
    {
      this.family = family;
      this.type = type;
      this.outline = baseLine;
      this.baseOffset = baseOffset;
      this.topOffset = topOffset;
      this.flipped = flipped;
      this.structural = structural;
      this.level = level;
      this.topLevel = topLevel;
      this.elements = elements;
      this.parameters = parameters.ToBase();
      this.element2DType = Element2DType.Wall;
    }

    /// <summary>
    /// SchemaBuilder constructor for a Revit wall
    /// </summary>
    /// <param name="family"></param>
    /// <param name="type"></param>
    /// <param name="baseLine"></param>
    /// <param name="level"></param>
    /// <param name="height"></param>
    /// <param name="baseOffset"></param>
    /// <param name="topOffset"></param>
    /// <param name="flipped"></param>
    /// <param name="structural"></param>
    /// <param name="elements"></param>
    /// <param name="parameters"></param>
    /// <remarks>Assign units when using this constructor due to <paramref name="height"/>, <paramref name="baseOffset"/>, and <paramref name="topOffset"/> params</remarks>
    [SchemaInfo("RevitWall by curve and height", "Creates an unconnected Revit wall.", "Revit", "Architecture")]
    public RevitWall(string family, string type,
      [SchemaMainParam] ICurve baseLine, Level level, double height, double baseOffset = 0, double topOffset = 0, bool flipped = false, bool structural = false,
      [SchemaParamInfo("Set in here any nested elements that this wall might have.")] List<Base> elements = null,
      List<Parameter> parameters = null)
    {
      this.family = family;
      this.type = type;
      this.outline = baseLine;
      this.height = height;
      this.baseOffset = baseOffset;
      this.topOffset = topOffset;
      this.flipped = flipped;
      this.structural = structural;
      this.level = level;
      this.elements = elements;
      this.parameters = parameters.ToBase();
      this.element2DType = Element2DType.Wall;
    }
  }

  public class RevitFaceWall : BuiltElement2D
  {
    public string family { get; set; }
    public string type { get; set; }
    public Surface surface { get; set; }
    public Level level { get; set; }
    public LocationLine locationLine { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }

    public RevitFaceWall() { }

    [SchemaInfo("RevitWall by face", "Creates a Revit wall from a surface.", "Revit", "Architecture")]
    public RevitFaceWall(string family, string type,
      [SchemaParamInfo("Surface or single face Brep to use")][SchemaMainParam] Brep surface,
      Level level, LocationLine locationLine = LocationLine.Interior,
      [SchemaParamInfo("Set in here any nested elements that this wall might have.")] List<Base> elements = null,
      List<Parameter> parameters = null)
    {
      this.family = family;
      this.type = type;
      this.surface = surface.Surfaces[0];
      this.locationLine = locationLine;
      this.level = level;
      this.elements = elements;
      this.parameters = parameters.ToBase();
      this.element2DType = Element2DType.Wall;
    }
  }

  public class RevitProfileWall : BuiltElement2D
  {
    public string family { get; set; }
    public string type { get; set; }
    public Polycurve profile { get; set; }
    public Level level { get; set; }
    public LocationLine locationLine { get; set; }
    public bool structural { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }

    public RevitProfileWall() { }

    [SchemaInfo("RevitWall by profile", "Creates a Revit wall from a profile.", "Revit", "Architecture")]
    public RevitProfileWall(string family, string type,
      [SchemaParamInfo("Profile to use")][SchemaMainParam] Polycurve profile, Level level,
      LocationLine locationLine = LocationLine.Interior, bool structural = false,
      [SchemaParamInfo("Set in here any nested elements that this wall might have.")] List<Base> elements = null,
      List<Parameter> parameters = null)
    {
      this.family = family;
      this.type = type;
      this.profile = profile;
      this.locationLine = locationLine;
      this.structural = structural;
      this.level = level;
      this.elements = elements;
      this.parameters = parameters.ToBase();
      this.element2DType = Element2DType.Wall;
    }
  }


  // [SchemaDescription("Not supported yet.")]
  // [SchemaIgnore]
  // public class RevitCurtainWall : Wall
  // {
  //   // TODO
  //   // What props do/can curtain walls have? - grid, mullions, etc.
  //
  //   [SchemaOptional]
  //   public bool flipped { get; set; }
  //
  //   [SchemaOptional]
  //   public Base parameters { get; set; }
  //
  //   [SchemaIgnore]
  //   public string elementId { get; set; }
  // }
  //
  // [SchemaDescription("Not supported yet.")]
  // [SchemaIgnore]
  // public class RevitWallByPoint : Base
  // {
  //   [SchemaOptional]
  //   public Base parameters { get; set; }
  //
  //   [SchemaIgnore]
  //   public string elementId { get; set; }
  // }
}
