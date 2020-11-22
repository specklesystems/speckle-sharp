using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements
{
  public class Element : Base
  {
    [SchemaOptional]
    public Mesh displayMesh { get; set; }

    [SchemaIgnore]
    public string units { get; set; }
  }

}
