using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements;

public class Level : Base
{
  //public List<Base> elements { get; set; }

  public Level() { }

  /// <summary>
  /// SchemaBuilder constructor for a Speckle level
  /// </summary>
  /// <param name="name"></param>
  /// <param name="elevation"></param>
  /// <remarks>Assign units when using this constructor due to <paramref name="elevation"/> param</remarks>
  [SchemaInfo("Level", "Creates a Speckle level", "BIM", "Architecture")]
  public Level(string name, double elevation)
  {
    this.name = name;
    this.elevation = elevation;
  }

  public string name { get; set; }
  public double elevation { get; set; }

  public string units { get; set; }
}
