using Objects.BuiltElements;
using Speckle.Core.Kits;

namespace Objects.Structural.GSA.Geometry;

public class GSAGridLine : GridLine
{
  public GSAGridLine() { }

  [SchemaInfo("GSAGridLine", "Creates a Speckle structural grid line for GSA", "GSA", "Geometry")]
  public GSAGridLine(int nativeId, string name, ICurve line)
  {
    this.nativeId = nativeId;
    label = name;
    baseLine = line;
  }

  public int nativeId { get; set; }
}
