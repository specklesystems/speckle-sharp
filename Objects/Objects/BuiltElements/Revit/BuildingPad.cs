using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.BuiltElements.Revit
{
  public class BuildingPad : Base, IDisplayMesh
  {
    public ICurve outline { get; set; }
    public List<ICurve> voids { get; set; } = new List<ICurve>();
    public string type { get; set; }
    public Level level { get; set; }
    public List<Parameter> parameters { get; set; }
    public string elementId { get; set; }

    [DetachProperty]
    public Mesh displayMesh { get; set; }

    public BuildingPad() { }
  }
}
