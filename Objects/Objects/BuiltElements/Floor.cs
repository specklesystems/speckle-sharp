using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.BuiltElements
{
  public class Floor : Base
  {

    public ICurve outline { get; set; }

    [SchemaOptional]
    public List<ICurve> voids { get; set; } = new List<ICurve>();

    [SchemaDescription("Set in here any nested elements that this level might have.")]
    [SchemaOptional]
    public List<Base> elements { get; set; }

    public Floor() { }
  }

}
namespace Objects.BuiltElements.Revit
{

  public class RevitFloor : Floor
  {
    [SchemaOptional]
    public bool structural { get; set; }

    [SchemaOptional]
    public string family { get; set; }

    [SchemaOptional]
    public string type { get; set; }

    [SchemaOptional]
    public Dictionary<string, object> parameters { get; set; }

    [SchemaIgnore]
    public Dictionary<string, object> typeParameters { get; set; }

    [SchemaIgnore]
    public string elementId { get; set; }

    [SchemaOptional]
    public Level level { get; set; }

    public RevitFloor() { }
  }
}