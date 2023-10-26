using System;
using Objects.Structural.CSI.Geometry;
using Objects.Geometry;

namespace Objects.Converter.CSI
{
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
      double[] x = null;
      double[] y = null;
      double[] z = null;
      Model.TendonObj.GetTendonGeometry(name, ref numberPoints, ref x, ref y, ref z);
      if (numberPoints != 0)
      {
        var polyLine = new Polycurve();
        for (int i = 0; i < numberPoints - 1; i++)
        {
          var pt1 = new Point(x[i], y[i], z[i]);
          var pt2 = new Point(x[i + 1], y[i + 1], z[i + 1]);
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
}
