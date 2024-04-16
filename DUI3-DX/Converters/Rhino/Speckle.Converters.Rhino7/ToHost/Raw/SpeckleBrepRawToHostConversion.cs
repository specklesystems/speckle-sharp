using Objects;
using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class SpeckleBrepRawToHostConversion : IRawConversion<SOG.Brep, RG.Brep>
{
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;
  private readonly IRawConversion<ICurve, RG.Curve> _curveConverter;
  private readonly IRawConversion<SOG.Surface, RG.Surface> _surfaceConverter;
  private readonly IRawConversion<SOG.Point, RG.Point3d> _pointConverter;
  private readonly IRawConversion<SOP.Interval, RG.Interval> _intervalConverter;

  public SpeckleBrepRawToHostConversion(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    IRawConversion<ICurve, RG.Curve> curveConverter,
    IRawConversion<SOG.Surface, RG.Surface> surfaceConverter,
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

    rhinoBrep.Repair(tolerance);

    return rhinoBrep;
  }

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

  private void ConvertSpeckleBrepFace(RG.Brep rhinoBrep, SOG.BrepFace speckleFace)
  {
    var f = rhinoBrep.Faces.Add(speckleFace.SurfaceIndex);
    f.OrientationIsReversed = speckleFace.OrientationReversed;
  }
}
