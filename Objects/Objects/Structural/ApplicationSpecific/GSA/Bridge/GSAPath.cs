using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.GSA.Bridge;

public class GSAPath : Base
{
  public GSAPath() { }

  [SchemaInfo(
    "GSAPath",
    "Creates a Speckle structural path for GSA (a path defines traffic lines along a bridge relative to an alignments, for influence analysis)",
    "GSA",
    "Bridge"
  )]
  public GSAPath(
    int nativeId,
    string name,
    PathType type,
    int group,
    GSAAlignment alignment,
    double left,
    double right,
    double factor,
    int numMarkedLanes
  )
  {
    this.nativeId = nativeId;
    this.name = name;
    this.type = type;
    this.group = group;
    this.alignment = alignment;
    this.left = left;
    this.right = right;
    this.factor = factor;
    this.numMarkedLanes = numMarkedLanes;
  }

  public int nativeId { get; set; }
  public string name { get; set; }
  public PathType type { get; set; }
  public int group { get; set; }

  [DetachProperty]
  public GSAAlignment alignment { get; set; }

  public double left { get; set; } //left / centre offset
  public double right { get; set; } //right offset / gauge
  public double factor { get; set; } //left factor
  public int numMarkedLanes { get; set; }
}

public enum PathType
{
  NotSet = 0,
  LANE,
  FOOTWAY,
  TRACK,
  VEHICLE,
  CWAY_1WAY,
  CWAY_2WAY
}
