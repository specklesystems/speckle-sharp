#if CIVIL2021
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using CivilDB = Autodesk.Civil.DatabaseServices;

using Polycurve = Objects.Geometry.Polycurve;

namespace Objects.Converter.AutocadCivil
{
  public partial class ConverterAutocadCivil
  {
    // featurelines
    public Polycurve FeatureLineToSpeckle(CivilDB.FeatureLine featureLine)
    {
      var polycurve = new Polycurve() { closed = featureLine.Closed };

      // extract segment curves
      var segments = new List<ICurve>();
      var exploded = new DBObjectCollection();
      featureLine.Explode(exploded);
      for (int i = 0; i < exploded.Count; i++)
        segments.Add((ICurve)ConvertToSpeckle(exploded[i]));
      polycurve.segments = segments;

      return polycurve;
    }
    public CivilDB.FeatureLine FeatureLineToNative(Polycurve polycurve)
    {
      return null;
    }
  }
}
#endif