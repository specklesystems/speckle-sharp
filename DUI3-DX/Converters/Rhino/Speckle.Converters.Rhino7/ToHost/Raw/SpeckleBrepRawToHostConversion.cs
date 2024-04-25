using Objects;
using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class SpeckleBrepRawToHostConversion : IRawConversion<SOG.Brep, RG.Brep>
{
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;
  private readonly IRawConversion<ICurve, RG.Curve> _curveConverter;
  private readonly IRawConversion<SOG.Surface, RG.NurbsSurface> _surfaceConverter;
  private readonly IRawConversion<SOG.Point, RG.Point3d> _pointConverter;
  private readonly IRawConversion<SOP.Interval, RG.Interval> _intervalConverter;

  public SpeckleBrepRawToHostConversion(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    IRawConversion<ICurve, RG.Curve> curveConverter,
    IRawConversion<SOG.Surface, RG.NurbsSurface> surfaceConverter,
    IRawConversion<SOG.Point, RG.Point3d> pointConverter,
    IRawConversion<SOP.Interval, RG.Interval> intervalConverter
  )
  {
    _contextStack = contextStack;
    _curveConverter = curveConverter;
    _surfaceConverter = surfaceConverter;
    _pointConverter = pointConverter;
    _intervalConverter = intervalConverter;
  }

  /// <summary>
  /// Converts a Speckle <see cref="SOG.Brep"/> to a Rhino <see cref="RG.Brep"/>.
  /// </summary>
  /// <remarks>
  /// This method converts a Speckle Brep object to its equivalent Rhino Brep representation.
  /// The conversion process includes converting individual curves, trims, surfaces, and vertices.
  /// The resulting Rhino Brep is returned.
  /// Note that the conversion does not cover all edge cases in Brep structures, therefore it is recommended to review the resulting Brep for robustness improvement.
  /// </remarks>
  /// <param name="target">The Speckle Brep object to be converted.</param>
  /// <returns>The equivalent Rhino Brep object.</returns>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  public RG.Brep RawConvert(SOG.Brep target)
  {
    var tolerance = _contextStack.Current.Document.ModelAbsoluteTolerance;

    var rhinoBrep = new RG.Brep();

    // Geometry goes in first, always. Order doesn't matter.
    target.Curve3D.ForEach(curve => rhinoBrep.AddEdgeCurve(_curveConverter.RawConvert(curve)));
    target.Curve2D.ForEach(curve => rhinoBrep.AddTrimCurve(_curveConverter.RawConvert(curve)));
    target.Surfaces.ForEach(surface => rhinoBrep.AddSurface(_surfaceConverter.RawConvert(surface)));
    target.Vertices.ForEach(vertex => rhinoBrep.Vertices.Add(_pointConverter.RawConvert(vertex), tolerance));

    // Order matters, first edges, then faces, finally loops.
    target.Edges.ForEach(edge => ConvertSpeckleBrepEdge(rhinoBrep, edge, tolerance));
    target.Faces.ForEach(face => ConvertSpeckleBrepFace(rhinoBrep, face));
    target.Loops.ForEach(loop => ConvertSpeckleBrepLoop(rhinoBrep, loop, tolerance));

    //poc: repair is quite slow, we're experimenting to see if we can avoid calling it
    // rhinoBrep.Repair(tolerance); // Repair fixes tolerance issues with the Brep if the scaling lead to some rounding error.

    return rhinoBrep;
  }

  /// <summary>
  /// Converts a Speckle <see cref="SOG.BrepLoop"/> to a Rhino <see cref="RG.BrepLoop"/> and adds it to the provided <see cref="RG.Brep"/>.
  /// </summary>
  /// <remarks>
  /// A <see cref="SOG.BrepLoop"/> consists of individual trims. There are special cases for singular trims and trims with defined edge indices.
  /// Note that edge cases in Brep structures are not fully covered by this method and should be reviewed for robustness improvement.
  /// This operation alters the state of the provided <see cref="RG.Brep"/> by adding a new loop.
  /// </remarks>
  /// <param name="rhinoBrep">The <see cref="RG.Brep"/> where the new loop will be added.</param>
  /// <param name="speckleLoop">The <see cref="SOG.BrepLoop"/> to be converted and added to <paramref name="rhinoBrep"/>.</param>
  /// <param name="tol">The tolerance factor used when adding trims and setting their tolerances.</param>
  private void ConvertSpeckleBrepLoop(RG.Brep rhinoBrep, SOG.BrepLoop speckleLoop, double tol)
  {
    var f = rhinoBrep.Faces[speckleLoop.FaceIndex];

    rhinoBrep.Loops.Add((RG.BrepLoopType)speckleLoop.Type, f);

    // POC: This works but it doesn't fully cover all Brep edge cases and could be the cause of some of our failed Rhino->Rhino breps.
    // We should check Rhino.Inside as they have similar code structure.
    speckleLoop.Trims
      .ToList()
      .ForEach(trim =>
      {
        RG.BrepTrim rhTrim;
        if (trim.EdgeIndex != -1)
        {
          rhTrim = rhinoBrep.Trims.Add(
            rhinoBrep.Edges[trim.EdgeIndex],
            trim.IsReversed,
            rhinoBrep.Loops[trim.LoopIndex],
            trim.CurveIndex
          );
        }
        else if (trim.TrimType == SOG.BrepTrimType.Singular)
        {
          rhTrim = rhinoBrep.Trims.AddSingularTrim(
            rhinoBrep.Vertices[trim.EndIndex],
            rhinoBrep.Loops[trim.LoopIndex],
            (RG.IsoStatus)trim.IsoStatus,
            trim.CurveIndex
          );
        }
        else
        {
          rhTrim = rhinoBrep.Trims.Add(trim.IsReversed, rhinoBrep.Loops[trim.LoopIndex], trim.CurveIndex);
        }

        rhTrim.IsoStatus = (RG.IsoStatus)trim.IsoStatus;
        rhTrim.TrimType = (RG.BrepTrimType)trim.TrimType;
        rhTrim.SetTolerances(tol, tol);
      });
  }

  /// <summary>
  /// Converts a Speckle BrepEdge into a Rhino BrepEdge within a Rhino Brep.
  /// </summary>
  /// <param name="rhinoBrep">The Rhino Brep to which the converted BrepEdge will be added.</param>
  /// <param name="speckleEdge">The Speckle BrepEdge to convert.</param>
  /// <param name="tolerance">The tolerance for the conversion.</param>
  /// <remarks>
  /// If the domain of the Speckle BrepEdge is null or matches the curve's domain, it is assumed that the edge
  /// is untrimmed, and hence added directly as a reference to the curve it points to.
  /// If the edge is trimmed, it is added based on vertices and subdomain using the supplied tolerance
  /// </remarks>
  private void ConvertSpeckleBrepEdge(RG.Brep rhinoBrep, SOG.BrepEdge speckleEdge, double tolerance)
  {
    if (
      speckleEdge.Domain == null
      || speckleEdge.Domain.start == speckleEdge.Curve.domain.start
        && speckleEdge.Domain.end == speckleEdge.Curve.domain.end
    )
    {
      // The edge is untrimmed, we can add it directly as a reference to the curve it points to.
      rhinoBrep.Edges.Add(speckleEdge.Curve3dIndex);
    }
    else
    {
      // The edge is trimmed, must be added based on vertices and subdomain
      rhinoBrep.Edges.Add(
        speckleEdge.StartIndex,
        speckleEdge.EndIndex,
        speckleEdge.Curve3dIndex,
        _intervalConverter.RawConvert(speckleEdge.Domain),
        tolerance
      );
    }
  }

  /// <summary>
  /// Converts a <see cref="SOG.BrepFace"/> into a <see cref="RG.BrepFace"/> and adds it to the provided <see cref="RG.Brep"/>.
  /// </summary>
  /// <param name="rhinoBrep">The Rhinoceros brep geometry to which the converted face is added.</param>
  /// <param name="speckleFace">The Speckle brep face to be converted and added.</param>
  private void ConvertSpeckleBrepFace(RG.Brep rhinoBrep, SOG.BrepFace speckleFace)
  {
    var f = rhinoBrep.Faces.Add(speckleFace.SurfaceIndex);
    f.OrientationIsReversed = speckleFace.OrientationReversed;
  }
}
