using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class SurfaceToHostConverter : ITypedConverter<SOG.Surface, IRhinoNurbsSurface>
{
  private readonly IRhinoSurfaceFactory _rhinoSurfaceFactory;

  public SurfaceToHostConverter(IRhinoSurfaceFactory rhinoSurfaceFactory)
  {
    _rhinoSurfaceFactory = rhinoSurfaceFactory;
  }

  /// <summary>
  /// Converts a raw Speckle surface to a Rhino NURBS surface.
  /// </summary>
  /// <param name="target">The raw Speckle surface to convert.</param>
  /// <returns>The converted Rhino NURBS surface.</returns>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  public IRhinoNurbsSurface Convert(SOG.Surface target)
  {
    // Create rhino surface
    var points = target.GetControlPoints().ToList();

    var result = _rhinoSurfaceFactory.Create(
      3,
      target.rational,
      target.degreeU + 1,
      target.degreeV + 1,
      points.Count,
      points[0].Count
    );

    // Set knot vectors
    var correctUKnots = GetCorrectKnots(target.knotsU, target.countU, target.degreeU);
    for (int i = 0; i < correctUKnots.Count; i++)
    {
      result.KnotsU.SetKnot(i,  correctUKnots[i]);
    }

    var correctVKnots = GetCorrectKnots(target.knotsV, target.countV, target.degreeV);
    for (int i = 0; i < correctVKnots.Count; i++)
    {
      result.KnotsV.SetKnot(i, correctVKnots[i]);
    }

    // Set control points
    for (var i = 0; i < points.Count; i++)
    {
      for (var j = 0; j < points[i].Count; j++)
      {
        var pt = points[i][j];
        result.Points.SetPoint(i, j, pt.x * pt.weight, pt.y * pt.weight, pt.z * pt.weight);
        result.Points.SetWeight(i, j, pt.weight);
      }
    }

    // Return surface
    return result;
  }

  private List<double> GetCorrectKnots(List<double> knots, int controlPointCount, int degree)
  {
    var correctKnots = knots;
    if (knots.Count == controlPointCount + degree + 1)
    {
      correctKnots.RemoveAt(0);
      correctKnots.RemoveAt(correctKnots.Count - 1);
    }

    return correctKnots;
  }
}
