using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.BuiltElements
{
  public class Wall : Base
  {
    public double height { get; set; }


    public ICurve baseLine { get; set; }

    public Wall() { }

    [SchemaInfo("Wall", "Creates a Speckle wall")]
    public Wall(double height, ICurve baseLine)
    {
      this.height = height;
      this.baseLine = baseLine;
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
    public List<Base> hostedElements { get; set; }
    public Dictionary<string, object> parameters { get; set; }
    public string elementId { get; set; }

    public RevitWall()
    {

    }

    [SchemaInfo("Wall by curve and levels", "Creates a Revit wall with a top and base level.")]
    public RevitWall(string family, string type,
      ICurve baseLine, Level level, Level topLevel, double baseOffset = 0, double topOffset = 0, bool flipped = false, bool structural = false,
      [SchemaParamInfo("Set in here any nested elements that this level might have.")] List<Base> hostedElements = null,
      Dictionary<string, object> parameters = null)
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
      this.hostedElements = hostedElements;
      this.parameters = parameters;
    }

    [SchemaInfo("Wall by curve and height", "Creates an unconnected Revit wall.")]
    public RevitWall(string family, string type,
      ICurve baseLine, Level level, double height, double baseOffset = 0, double topOffset = 0, bool flipped = false, bool structural = false,
      [SchemaParamInfo("Set in here any nested elements that this level might have.")] List<Base> hostedElements = null,
      Dictionary<string, object> parameters = null)
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
      this.hostedElements = hostedElements;
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
  //   public Dictionary<string, object> parameters { get; set; }
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
  //   public Dictionary<string, object> parameters { get; set; }
  //
  //   [SchemaIgnore]
  //   public string elementId { get; set; }
  // }
}