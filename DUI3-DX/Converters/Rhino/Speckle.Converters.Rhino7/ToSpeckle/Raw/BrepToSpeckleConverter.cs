using Objects;
using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Logging;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class BrepToSpeckleConverter : IRawConversion<RG.Brep, SOG.Brep>
{
  private readonly IRawConversion<RG.Point3d, SOG.Point> _pointConverter;
  private readonly IRawConversion<RG.Curve, ICurve> _curveConverter;
  private readonly IRawConversion<RG.NurbsSurface, SOG.Surface> _surfaceConverter;
  private readonly IRawConversion<RG.Mesh, SOG.Mesh> _meshConverter;
  private readonly IRawConversion<RG.Box, SOG.Box> _boxConverter;
  private readonly IRawConversion<RG.Interval, SOP.Interval> _intervalConverter;
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public BrepToSpeckleConverter(
    IRawConversion<RG.Point3d, SOG.Point> pointConverter,
    IRawConversion<RG.Curve, ICurve> curveConverter,
    IRawConversion<RG.NurbsSurface, SOG.Surface> surfaceConverter,
    IRawConversion<RG.Mesh, SOG.Mesh> meshConverter,
    IRawConversion<RG.Box, SOG.Box> boxConverter,
    IRawConversion<RG.Interval, SOP.Interval> intervalConverter,
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack
  )
  {
    _pointConverter = pointConverter;
    _curveConverter = curveConverter;
    _surfaceConverter = surfaceConverter;
    _meshConverter = meshConverter;
    _boxConverter = boxConverter;
    _intervalConverter = intervalConverter;
    _contextStack = contextStack;
  }

  public SOG.Brep RawConvert(RG.Brep target)
  {
    var tol = _contextStack.Current.Document.ModelAbsoluteTolerance;
    target.Repair(tol);

    // POC: CNX-9276 This should come as part of the user settings in the context object.
    // if (PreprocessGeometry)
    // {
    //   brep = BrepEncoder.ToRawBrep(brep, 1.0, Doc.ModelAngleToleranceRadians, Doc.ModelRelativeTolerance);
    // }

    // get display mesh and attach render material to it if it exists
    var displayMesh = GetBrepDisplayMesh(target);
    var displayValue = new List<SOG.Mesh>();
    if (displayMesh != null)
    {
      displayValue.Add(_meshConverter.RawConvert(displayMesh));
    }

    // POC: CNX-9277 Swap input material for something coming from the context.
    // if (displayValue != null && mat != null)
    // {
    //   displayValue["renderMaterial"] = mat;
    // }

    // Vertices, uv curves, 3d curves and surfaces
    var vertices = target.Vertices.Select(vertex => _pointConverter.RawConvert(vertex.Location)).ToList();
    var curves3d = target.Curves3D.Select(curve3d => _curveConverter.RawConvert(curve3d)).ToList();
    var surfaces = target.Surfaces.Select(srf => _surfaceConverter.RawConvert(srf.ToNurbsSurface())).ToList();

    List<ICurve> curves2d;
    using (_contextStack.Push(Units.None))
    {
      // Curves2D are unitless, so we convert them within a new pushed context with None units.
      curves2d = target.Curves2D.Select(curve2d => _curveConverter.RawConvert(curve2d)).ToList();
    }

    var speckleBrep = new SOG.Brep
    {
      Vertices = vertices,
      Curve3D = curves3d,
      Curve2D = curves2d,
      Surfaces = surfaces,
      displayValue = displayValue,
      IsClosed = target.IsSolid,
      Orientation = (SOG.BrepOrientation)target.SolidOrientation,
      volume = target.IsSolid ? target.GetVolume() : 0,
      area = target.GetArea(),
      bbox = _boxConverter.RawConvert(new RG.Box(target.GetBoundingBox(false))),
      units = _contextStack.Current.SpeckleUnits
    };

    // Brep non-geometry types
    var faces = ConvertBrepFaces(target, speckleBrep);
    var edges = ConvertBrepEdges(target, speckleBrep);
    var loops = ConvertBrepLoops(target, speckleBrep);
    var trims = ConvertBrepTrims(target, speckleBrep);

    return speckleBrep;
  }

  private static List<SOG.BrepFace> ConvertBrepFaces(RG.Brep brep, SOG.Brep speckleParent) =>
    brep.Faces
      .Select(
        f =>
          new SOG.BrepFace(
            speckleParent,
            f.SurfaceIndex,
            f.Loops.Select(l => l.LoopIndex).ToList(),
            f.OuterLoop.LoopIndex,
            f.OrientationIsReversed
          )
      )
      .ToList();

  private List<SOG.BrepEdge> ConvertBrepEdges(RG.Brep brep, SOG.Brep speckleParent) =>
    brep.Edges
      .Select(
        edge =>
          new SOG.BrepEdge(
            speckleParent,
            edge.EdgeCurveIndex,
            edge.TrimIndices(),
            edge.StartVertex?.VertexIndex ?? -1,
            edge.EndVertex?.VertexIndex ?? -1,
            edge.ProxyCurveIsReversed,
            _intervalConverter.RawConvert(edge.Domain)
          )
      )
      .ToList();

  private List<SOG.BrepTrim> ConvertBrepTrims(RG.Brep brep, SOG.Brep speckleParent) =>
    brep.Trims
      .Select(trim =>
      {
        var t = new SOG.BrepTrim(
          speckleParent,
          trim.Edge?.EdgeIndex ?? -1,
          trim.Face.FaceIndex,
          trim.Loop.LoopIndex,
          trim.TrimCurveIndex,
          (int)trim.IsoStatus,
          (SOG.BrepTrimType)trim.TrimType,
          trim.IsReversed(),
          trim.StartVertex.VertexIndex,
          trim.EndVertex.VertexIndex
        )
        {
          Domain = _intervalConverter.RawConvert(trim.Domain)
        };

        return t;
      })
      .ToList();

  private List<SOG.BrepLoop> ConvertBrepLoops(RG.Brep brep, SOG.Brep speckleParent) =>
    brep.Loops
      .Select(
        loop =>
          new SOG.BrepLoop(
            speckleParent,
            loop.Face.FaceIndex,
            loop.Trims.Select(t => t.TrimIndex).ToList(),
            (SOG.BrepLoopType)loop.LoopType
          )
      )
      .ToList();

  private RG.Mesh? GetBrepDisplayMesh(RG.Brep brep)
  {
    var joinedMesh = new RG.Mesh();

    // get from settings
    //Settings.TryGetValue("sendMeshSetting", out string meshSetting);

    RG.MeshingParameters mySettings = new(0.05, 0.05);
    // switch (SelectedMeshSettings)
    // {
    //   case MeshSettings.CurrentDoc:
    //     mySettings = RH.MeshingParameters.DocumentCurrentSetting(Doc);
    //     break;
    //   case MeshSettings.Default:
    //   default:
    //     mySettings = new RH.MeshingParameters(0.05, 0.05);
    //     break;
    // }

    try
    {
      joinedMesh.Append(RG.Mesh.CreateFromBrep(brep, mySettings));
      return joinedMesh;
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      return null;
    }
  }
}
