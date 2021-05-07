#if (CIVIL2021 || CIVIL2022)
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using CivilDB = Autodesk.Civil.DatabaseServices;

using Interval = Objects.Primitive.Interval;
using Polycurve = Objects.Geometry.Polycurve;
using Brep = Objects.Geometry.Brep;

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

      // TODO: additional params to attach
      var points = featureLine.GetPoints(Autodesk.Civil.FeatureLinePointType.AllPoints);
      var grade = new Interval(featureLine.MinGrade, featureLine.MaxGrade);
      var elevation = new Interval(featureLine.MinElevation, featureLine.MaxElevation);
      var name = featureLine.DisplayName;

      return polycurve;
    }
    public CivilDB.FeatureLine FeatureLineToNative(Polycurve polycurve)
    {
      return null;
    }

    // alignments
    public Curve AlignmentToSpeckle(CivilDB.Alignment alignment)
    {
      var baseCurve = alignment.BaseCurve;
      return null;
    }

    // 3D solids
    //public Brep SolidToSpeckle(Solid3d solid)
    //{
    //return null;
    //}
  }
}
#endif