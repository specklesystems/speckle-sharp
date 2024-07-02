using Objects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using System.Collections;

namespace Speckle.Converters.RevitShared.ToSpeckle;

[NameAndRankValue(nameof(SOBR.Curve.ModelCurve), 0)]
public class ModelCurveToHostTopLevelConverter : BaseTopLevelConverterToHost<SOBR.Curve.ModelCurve, DB.ModelCurve[]>
{
  private readonly ITypedConverter<ICurve, DB.CurveArray> _curveConverter;
  private readonly IRevitConversionContextStack _contextStack;

  public ModelCurveToHostTopLevelConverter(
    ITypedConverter<ICurve, DB.CurveArray> curveConverter,
    IRevitConversionContextStack conversionContext
  )
  {
    _curveConverter = curveConverter;
    _contextStack = conversionContext;
  }

  public override DB.ModelCurve[] Convert(SOBR.Curve.ModelCurve target) =>
    ModelCurvesFromEnumerator(_curveConverter.Convert(target.baseCurve).GetEnumerator(), target.baseCurve).ToArray();

  private IEnumerable<DB.ModelCurve> ModelCurvesFromEnumerator(IEnumerator curveEnum, ICurve speckleLine)
  {
    while (curveEnum.MoveNext() && curveEnum.Current != null)
    {
      var curve = (DB.Curve)curveEnum.Current;
      // Curves must be bound in order to be valid model curves
      if (!curve.IsBound)
      {
        curve.MakeBound(speckleLine.domain.start ?? 0, speckleLine.domain.end ?? Math.PI * 2);
      }

      if (_contextStack.Current.Document.IsFamilyDocument)
      {
        yield return _contextStack.Current.Document.FamilyCreate.NewModelCurve(
          curve,
          NewSketchPlaneFromCurve(curve, _contextStack.Current.Document)
        );
      }
      else
      {
        yield return _contextStack.Current.Document.Create.NewModelCurve(
          curve,
          NewSketchPlaneFromCurve(curve, _contextStack.Current.Document)
        );
      }
    }
  }

  /// <summary>
  /// Credits: Grevit
  /// Creates a new Sketch Plane from a Curve
  /// https://github.com/grevit-dev/Grevit/blob/3c7a5cc198e00dfa4cc1e892edba7c7afd1a3f84/Grevit.Revit/Utilities.cs#L402
  /// </summary>
  /// <param name="curve">Curve to get plane from</param>
  /// <returns>Plane of the curve</returns>
  private DB.SketchPlane NewSketchPlaneFromCurve(DB.Curve curve, DB.Document doc)
  {
    DB.XYZ startPoint = curve.GetEndPoint(0);
    DB.XYZ endPoint = curve.GetEndPoint(1);

    // If Start end Endpoint are the same check further points.
    int i = 2;
    while (startPoint == endPoint && endPoint != null)
    {
      endPoint = curve.GetEndPoint(i);
      i++;
    }

    // Plane to return
    DB.Plane plane;

    // If Z Values are equal the Plane is XY
    if (startPoint.Z == endPoint.NotNull().Z)
    {
      plane = DB.Plane.CreateByNormalAndOrigin(DB.XYZ.BasisZ, startPoint);
    }
    // If X Values are equal the Plane is YZ
    else if (startPoint.X == endPoint.X)
    {
      plane = DB.Plane.CreateByNormalAndOrigin(DB.XYZ.BasisX, startPoint);
    }
    // If Y Values are equal the Plane is XZ
    else if (startPoint.Y == endPoint.Y)
    {
      plane = DB.Plane.CreateByNormalAndOrigin(DB.XYZ.BasisY, startPoint);
    }
    // Otherwise the Planes Normal Vector is not X,Y or Z.
    // We draw lines from the Origin to each Point and use the Plane this one spans up.
    else
    {
      using DB.CurveArray curves = new();
      curves.Append(curve);
      curves.Append(DB.Line.CreateBound(new DB.XYZ(0, 0, 0), startPoint));
      curves.Append(DB.Line.CreateBound(endPoint, new DB.XYZ(0, 0, 0)));

      plane = DB.Plane.CreateByThreePoints(startPoint, new DB.XYZ(0, 0, 0), endPoint);
    }

    return DB.SketchPlane.Create(doc, plane);
  }
}
