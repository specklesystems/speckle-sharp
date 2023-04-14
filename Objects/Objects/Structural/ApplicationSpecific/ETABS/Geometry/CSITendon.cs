using Objects.Geometry;
using Objects.Structural.CSI.Properties;
using Speckle.Core.Models;

namespace Objects.Structural.CSI.Geometry;

public class CSITendon : CSIElement1D
{
  public CSITendon(string name, Polycurve polycurve, CSITendonProperty CSITendonProperty)
  {
    this.name = name;
    this.polycurve = polycurve;
    CSITendonProperty = CSITendonProperty;
  }

  public CSITendon() { }

  public Polycurve polycurve { get; set; }

  [DetachProperty]
  public CSITendonProperty CSITendonProperty { get; set; }
}
