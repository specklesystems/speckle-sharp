using System.Collections.Generic;

namespace StructuralUtilities.PolygonMesher;

internal class IndexPair : IndexSet
{
  public IndexPair(int index1, int index2)
    : base(new List<int>() { index1, index2 }) { }

  public int? Other(int index)
  {
    return (Indices[0] == index)
      ? Indices[1]
      : (Indices[1] == index)
        ? (int?)Indices[0]
        : null;
  }
}
