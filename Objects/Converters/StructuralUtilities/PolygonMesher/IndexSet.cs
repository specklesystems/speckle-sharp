using System.Collections.Generic;
using System.Linq;

namespace StructuralUtilities.PolygonMesher;

internal abstract class IndexSet
{
  public readonly int[] Indices;

  public IndexSet(List<int> values)
  {
    Indices = values.OrderBy(v => v).ToArray();
  }

  public bool Contains(int index)
  {
    return Indices.Any(i => i == index);
  }

  public bool Matches(IndexSet other)
  {
    return Indices.SequenceEqual(other.Indices);
  }
}
