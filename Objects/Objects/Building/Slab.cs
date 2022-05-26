using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.Building
{
  // TODO: think of better name for this
  public class Slab : OutlineBasedElement
  {
    public SlabType slabType { get; set; } = SlabType.Slab;

    [DetachProperty] public List<ICurve> voids { get; set; } = new List<ICurve>();

    public double slope { get; set; }
  }

  public enum SlabType
  {
    Floor,
    Ceiling,
    Slab
  }
}
