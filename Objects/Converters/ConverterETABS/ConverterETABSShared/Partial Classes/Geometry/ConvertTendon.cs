using System;
using System.Collections.Generic;
using System.Text;
using ETABSv1;
using Objects.Structural.Geometry;
using Objects.Structural.Analysis;
using Objects.Structural.ETABS.Properties;
using Objects.Structural.ETABS.Geometry;
using Objects.Geometry;
using System.Linq;

namespace Objects.Converter.ETABS
{
  public partial class ConverterETABS
  {
  public void ETABSTendonToSpeckle(ETABSTendon tendon){

      throw new NotSupportedException();
  }
  public ETABSTendon ETABSTendonToSpeckle(string name){
      var speckleETABSTendon = new ETABSTendon();
      int numberPoints = 0;
      double[] X = null;
      double[] Y = null;
      double[] Z = null;
      Model.TendonObj.GetTendonGeometry(name, ref numberPoints, ref X, ref Y, ref Z);
      if(numberPoints !=  0 ){
        var polyLine = new Polycurve();
      for(int i = 0; i< numberPoints-1; i++){
          var pt1 = new Point(X[i], Y[i], Z[i]);
          var pt2 = new Point(X[i+1], Y[i+1], Z[i+1]);
          var line = new Line(pt1, pt2);
          polyLine.segments.Add(line);
      }
        speckleETABSTendon.polycurve = polyLine;
      }
      string tendonProp = null;
      Model.TendonObj.GetProperty(name, ref tendonProp);
      speckleETABSTendon.ETABSTendonProperty = TendonPropToSpeckle(tendonProp);
      SpeckleModel.elements.Add(speckleETABSTendon);
      return speckleETABSTendon;

  }
  }
}
