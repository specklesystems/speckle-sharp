using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.BuiltElements
{
  public class Wall : Base, IDisplayMesh
  {
    public double height { get; set; }

    [DetachProperty]
    public List<Base> elements { get; set; }
    public ICurve baseLine { get; set; }

    [DetachProperty]
    public Mesh displayMesh { get; set; }

    public Wall() { }

    /// <summary>
    /// SchemaBuilder constructor for a Speckle wall
    /// </summary>
    /// <param name="height"></param>
    /// <param name="baseLine"></param>
    /// <param name="elements"></param>
    /// <remarks>Assign units when using this constructor due to <paramref name="height"/> param</remarks>
    [SchemaInfo("Wall", "Creates a Speckle wall")]
    public Wall(double height, [SchemaMainParam] ICurve baseLine,
      [SchemaParamInfo("Any nested elements that this wall might have")] List<Base> elements = null)
    {
      this.height = height;
      this.baseLine = baseLine;
      this.elements = elements;
    }
  }
}

namespace Objects.BuiltElements.Revit
{
  public class RevitWall : Wall
  {
    public string family { get; set; }
    public string type { get; set; }
    public double baseOffset { get; set; }
    public double topOffset { get; set; }
    public bool flipped { get; set; }
    public bool structural { get; set; }
    public Level level { get; set; }
    public Level topLevel { get; set; }
    public List<Parameter> parameters { get; set; }
    public string elementId { get; set; }

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
    [SchemaInfo("Wall by curve and levels", "Creates a Revit wall with a top and base level.")]
    public RevitWall(string family, string type,
      [SchemaMainParam] ICurve baseLine, Level level, Level topLevel, double baseOffset = 0, double topOffset = 0, bool flipped = false, bool structural = false,
      [SchemaParamInfo("Set in here any nested elements that this level might have.")] List<Base> elements = null,
      List<Parameter> parameters = null)
    {
      this.family = family;
      this.type = type;
      this.baseLine = baseLine;
      this.baseOffset = baseOffset;
      this.topOffset = topOffset;
      this.flipped = flipped;
      this.structural = structural;
      this.level = level;
      this.topLevel = topLevel;
      this.elements = elements;
      this.parameters = parameters;
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
    [SchemaInfo("Wall by curve and height", "Creates an unconnected Revit wall.")]
    public RevitWall(string family, string type,
      [SchemaMainParam] ICurve baseLine, Level level, double height, double baseOffset = 0, double topOffset = 0, bool flipped = false, bool structural = false,
      [SchemaParamInfo("Set in here any nested elements that this level might have.")] List<Base> elements = null,
      List<Parameter> parameters = null)
    {
      this.family = family;
      this.type = type;
      this.baseLine = baseLine;
      this.height = height;
      this.baseOffset = baseOffset;
      this.topOffset = topOffset;
      this.flipped = flipped;
      this.structural = structural;
      this.level = level;
      this.elements = elements;
      this.parameters = parameters;
    }
  }

  public class RevitFaceWall : Wall
  {
    public string family { get; set; }
    public string type { get; set; }
    public Surface surface { get; set; }
    public Level level { get; set; }
    public LocationLine locationLine { get; set; }
    public List<Parameter> parameters { get; set; }
    public string elementId { get; set; }

    public RevitFaceWall() { }

    [SchemaInfo("Wall by face", "Creates a Revit wall from a surface.")]
    public RevitFaceWall(string family, string type,
      [SchemaParamInfo("Surface or single face Brep to use")][SchemaMainParam] Brep surface,
      Level level, LocationLine locationLine = LocationLine.Interior,
      [SchemaParamInfo("Set in here any nested elements that this level might have.")] List<Base> elements = null,
      List<Parameter> parameters = null)
    {
      this.family = family;
      this.type = type;
      this.surface = surface.Surfaces[0];
      this.locationLine = locationLine;
      this.level = level;
      this.elements = elements;
      this.parameters = parameters;
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
  //   public List<Parameter> parameters { get; set; }
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
  //   public List<Parameter> parameters { get; set; }
  //
  //   [SchemaIgnore]
  //   public string elementId { get; set; }
  // }
}