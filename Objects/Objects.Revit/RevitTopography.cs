using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.Revit
{
  public class RevitTopography : Base, IBaseRevitElement, ITopography
  {
    public Mesh baseGeometry { get; set; } = new Mesh();

    public string elementId { get; set; }
  }
}
