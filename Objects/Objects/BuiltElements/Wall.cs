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
  }
}

namespace Objects.BuiltElements.Revit
{
  public class RevitWall : Wall
  {
    public string type { get; set; }

    [SchemaOptional]
    public double baseOffset { get; set; }

    [SchemaOptional]
    public double topOffset { get; set; }

    [SchemaOptional]
    public bool flipped { get; set; }

    [SchemaOptional]
    public bool structural { get; set; }

    [SchemaOptional]
    public Level level { get; set; }

    [SchemaOptional]
    [SchemaDescription("Setting the top level constraint on a wall will override its height parameter.")]
    public Level topLevel { get; set; }

    [SchemaDescription("Set in here any nested elements that this level might have.")]
    [SchemaOptional]
    public List<Base> hostedElements { get; set; }

    [SchemaOptional]
    public Dictionary<string, object> parameters { get; set; }

    [SchemaIgnore]
    public string elementId { get; set; }

    public RevitWall()
    {

    }

    [SchemaInfo("By curve and levels", "Creates a Revit wall with a top and base level.")]
    public RevitWall(
      [SchemaParamInfo("The Revit wall type, it must exist in a Revit document when receiving the wall, otherwise a default will be used.")] string type,
      ICurve baseLine, Level level, Level topLevel, double baseOffset = 0, double topOffset = 0, bool flipped = false, bool structural = false, List<Base> hostedElements = null, Dictionary<string, object> parameters = null)
    {
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

    [SchemaInfo("By curve and height", "Creates an unconnected Revit wall.")]
    public RevitWall(
      [SchemaParamInfo("The Revit wall type, it must exist in a Revit document when receiving the wall, otherwise a default will be used.")] string type,
      ICurve baseLine, Level level, double height, double baseOffset = 0, double topOffset = 0, bool flipped = false, bool structural = false, List<Base> hostedElements = null, Dictionary<string, object> parameters = null)
    {
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