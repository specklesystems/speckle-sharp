using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements
{
  public class Element : Base
  {
    [SchemaOptional]
    public Mesh displayMesh { get; set; } = new Mesh();

    [SchemaIgnore]
    public string linearUnits { get; set; }
  }

}
