using Objects.Primitive;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.Geometry;

/// <summary>
/// Represents a UV Trim curve for one of the <see cref="Brep"/>'s surfaces.
/// </summary>
public class BrepTrim : Base
{
  public BrepTrim() { }

  public BrepTrim(
    Brep brep,
    int edgeIndex,
    int faceIndex,
    int loopIndex,
    int curveIndex,
    int isoStatus,
    BrepTrimType trimType,
    bool reversed,
    int startIndex,
    int endIndex
  )
  {
    Brep = brep;
    EdgeIndex = edgeIndex;
    FaceIndex = faceIndex;
    LoopIndex = loopIndex;
    CurveIndex = curveIndex;
    IsoStatus = isoStatus;
    TrimType = trimType;
    IsReversed = reversed;
    StartIndex = startIndex;
    EndIndex = endIndex;
  }

  [JsonIgnore]
  public Brep Brep { get; set; }

  public int EdgeIndex { get; set; }
  public int StartIndex { get; set; }
  public int EndIndex { get; set; }
  public int FaceIndex { get; set; }
  public int LoopIndex { get; set; }
  public int CurveIndex { get; set; }
  public int IsoStatus { get; set; }
  public BrepTrimType TrimType { get; set; }
  public bool IsReversed { get; set; }

  public Interval Domain { get; set; } = new(0, 1);

  [JsonIgnore]
  public BrepFace Face => Brep.Faces[FaceIndex];

  [JsonIgnore]
  public BrepLoop Loop => Brep.Loops[LoopIndex];

  [JsonIgnore]
  public BrepEdge? Edge => EdgeIndex != -1 ? Brep.Edges[EdgeIndex] : null;

  [JsonIgnore]
  public ICurve Curve2d => Brep.Curve2D[CurveIndex];
}
