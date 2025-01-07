using System;
using Objects.Geometry;
using Objects.Structural.CSI.Geometry;

namespace Objects.Converter.CSI;

public partial class ConverterCSI
{
  public void CSITendonToSpeckle(CSITendon tendon)
  {
    throw new NotSupportedException();
  }

  public CSITendon CSITendonToSpeckle(string name)
  {
    var speckleCSITendon = new CSITendon();
    int numberPoints = 0;
    double[] X = null;
    double[] Y = null;
    double[] Z = null;
    Model.TendonObj.GetTendonGeometry(name, ref numberPoints, ref X, ref Y, ref Z);
    if (numberPoints != 0)
    {
      var polyLine = new Polycurve();
      for (int i = 0; i < numberPoints - 1; i++)
      {
        var pt1 = new Point(X[i], Y[i], Z[i]);
        var pt2 = new Point(X[i + 1], Y[i + 1], Z[i + 1]);
        var line = new Line(pt1, pt2);
        polyLine.segments.Add(line);
      }
      speckleCSITendon.polycurve = polyLine;
    }
    string tendonProp = null;
    Model.TendonObj.GetProperty(name, ref tendonProp);
    speckleCSITendon.CSITendonProperty = TendonPropToSpeckle(tendonProp);
    SpeckleModel.elements.Add(speckleCSITendon);
    return speckleCSITendon;
  }
}
