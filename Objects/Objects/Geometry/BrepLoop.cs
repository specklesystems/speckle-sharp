using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.Geometry;

/// <summary>
/// Represents a UV Trim Closed Loop on one of the <see cref="Brep"/>'s surfaces.
/// </summary>
public class BrepLoop : Base
{
  public BrepLoop() { }

  public BrepLoop(Brep brep, int faceIndex, List<int> trimIndices, BrepLoopType type)
  {
    Brep = brep;
    FaceIndex = faceIndex;
    TrimIndices = trimIndices;
    Type = type;
  }

  [JsonIgnore]
  public Brep Brep { get; set; }

  public int FaceIndex { get; set; }
  public List<int> TrimIndices { get; set; }
  public BrepLoopType Type { get; set; }

  [JsonIgnore]
  public BrepFace Face => Brep.Faces[FaceIndex];

  [JsonIgnore]
  public List<BrepTrim> Trims => TrimIndices.Select(i => Brep.Trims[i]).ToList();
}
