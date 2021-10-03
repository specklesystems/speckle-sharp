using System.Collections.Generic;

namespace SpeckleStructuralClasses.PolygonMesher
{
  internal class TriangleIndexSet : IndexSet
  {
    public TriangleIndexSet(int index1, int index2, int index3) : base(new List<int>() { index1, index2, index3 })
    {
    }
  }
}
