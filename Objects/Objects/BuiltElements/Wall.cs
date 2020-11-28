using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements
{
  public class Wall : Base
  {
    public double height { get; set; }

    public ICurve baseLine { get; set; }

    [SchemaDescription("Set in here any nested elements that this level might have.")]
    [SchemaOptional]
    public List<Base> elements { get; set; }

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

    [SchemaOptional]
    public Dictionary<string, object> parameters { get; set; }

    [SchemaIgnore]
    public string elementId { get; set; }
  }

  public class RevitCurtainWall : Wall
  {
    // TODO
    // What props do/can curtain walls have? - grid, mullions, etc.

    [SchemaOptional]
    public bool flipped { get; set; }

    [SchemaOptional]
    public Dictionary<string, object> parameters { get; set; }

    [SchemaIgnore]
    public string elementId { get; set; }
  }
}