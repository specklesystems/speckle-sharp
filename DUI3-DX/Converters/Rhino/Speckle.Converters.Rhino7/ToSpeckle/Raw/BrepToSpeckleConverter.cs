using Objects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class BrepToSpeckleConverter : ITypedConverter<IRhinoBrep, SOG.Brep>
{
  private readonly ITypedConverter<IRhinoPoint3d, SOG.Point> _pointConverter;
  private readonly ITypedConverter<IRhinoCurve, ICurve> _curveConverter;
  private readonly ITypedConverter<IRhinoNurbsSurface, SOG.Surface> _surfaceConverter;
  private readonly ITypedConverter<IRhinoMesh, SOG.Mesh> _meshConverter;
  private readonly ITypedConverter<IRhinoBox, SOG.Box> _boxConverter;
  private readonly ITypedConverter<IRhinoInterval, SOP.Interval> _intervalConverter;
  private readonly IConversionContextStack<IRhinoDoc, RhinoUnitSystem> _contextStack;
  private readonly IRhinoBoxFactory _rhinoBoxFactory;
  private readonly IRhinoMeshFactory _rhinoMeshFactory;

  public BrepToSpeckleConverter(
    ITypedConverter<IRhinoPoint3d, SOG.Point> pointConverter,
    ITypedConverter<IRhinoCurve, ICurve> curveConverter,
    ITypedConverter<IRhinoNurbsSurface, SOG.Surface> surfaceConverter,
    ITypedConverter<IRhinoMesh, SOG.Mesh> meshConverter,
    ITypedConverter<IRhinoBox, SOG.Box> boxConverter,
    ITypedConverter<IRhinoInterval, SOP.Interval> intervalConverter,
    IConversionContextStack<IRhinoDoc, RhinoUnitSystem> contextStack,
    IRhinoBoxFactory rhinoBoxFactory,
    IRhinoMeshFactory rhinoMeshFactory
  )
  {
    _pointConverter = pointConverter;
    _curveConverter = curveConverter;
    _surfaceConverter = surfaceConverter;
    _meshConverter = meshConverter;
    _boxConverter = boxConverter;
    _intervalConverter = intervalConverter;
    _contextStack = contextStack;
    _rhinoBoxFactory = rhinoBoxFactory;
    _rhinoMeshFactory = rhinoMeshFactory;
  }

  /// <summary>
  /// Converts a Brep object to a Speckle Brep object.
  /// </summary>
  /// <param name="target">The Brep object to convert.</param>
  /// <returns>The converted Speckle Brep object.</returns>
  public SOG.Brep Convert(IRhinoBrep target)
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
      displayValue.Add(_meshConverter.Convert(displayMesh));
    }

    // POC: CNX-9277 Swap input material for something coming from the context.
    // if (displayValue != null && mat != null)
    // {
    //   displayValue["renderMaterial"] = mat;
    // }

    // Vertices, uv curves, 3d curves and surfaces
    var vertices = target.Vertices.Select(vertex => _pointConverter.Convert(vertex.Location)).ToList();
    var curves3d = target.Curves3D.Select(curve3d => _curveConverter.Convert(curve3d)).ToList();
    var surfaces = target.Surfaces.Select(srf => _surfaceConverter.Convert(srf.ToNurbsSurface())).ToList();

    List<ICurve> curves2d;
    using (_contextStack.Push(Units.None))
    {
      // Curves2D are unitless, so we convert them within a new pushed context with None units.
      curves2d = target.Curves2D.Select(curve2d => _curveConverter.Convert(curve2d)).ToList();
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
      bbox = _boxConverter.Convert(_rhinoBoxFactory.CreateBox(target.GetBoundingBox(false))),
      units = _contextStack.Current.SpeckleUnits
    };

    // Brep non-geometry types
    var faces = ConvertBrepFaces(target, speckleBrep);
    var edges = ConvertBrepEdges(target, speckleBrep);
    var loops = ConvertBrepLoops(target, speckleBrep);
    var trims = ConvertBrepTrims(target, speckleBrep);

    speckleBrep.Faces = faces;
    speckleBrep.Edges = edges;
    speckleBrep.Loops = loops;
    speckleBrep.Trims = trims;
    return speckleBrep;
  }

  private static List<SOG.BrepFace> ConvertBrepFaces(IRhinoBrep brep, SOG.Brep speckleParent) =>
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

  private List<SOG.BrepEdge> ConvertBrepEdges(IRhinoBrep brep, SOG.Brep speckleParent) =>
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
            _intervalConverter.Convert(edge.Domain)
          )
      )
      .ToList();

  private List<SOG.BrepTrim> ConvertBrepTrims(IRhinoBrep brep, SOG.Brep speckleParent) =>
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
          trim.StartVertex?.VertexIndex ?? -1,
          trim.EndVertex?.VertexIndex ?? -1
        )
        {
          Domain = _intervalConverter.Convert(trim.Domain)
        };

        return t;
      })
      .ToList();

  private List<SOG.BrepLoop> ConvertBrepLoops(IRhinoBrep brep, SOG.Brep speckleParent) =>
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

  private IRhinoMesh? GetBrepDisplayMesh(IRhinoBrep brep)
  {
    var joinedMesh = _rhinoMeshFactory.Create();

    // get from settings
    //Settings.TryGetValue("sendMeshSetting", out string meshSetting);

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
      joinedMesh.Append(_rhinoMeshFactory.CreateFromBrep(brep, 0.05, 0.05));
      return joinedMesh;
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      return null;
    }
  }
}
