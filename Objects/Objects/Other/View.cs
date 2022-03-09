#nullable enable

using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.Other
{
  public class View : Base
  {
    // TODO: do we want sensible defaults for everything?
    public string name { get; set; }
    public string units { get; set; } = Units.Millimeters;
    public ViewType viewType { get; set; } = ViewType.Perspective;
    public Transform transform { get; set; } = new Transform();
    public double fov { get; set; }
    public double? fovY { get; set; }
    public double? orthoScale { get; set; }
    public double aspectX { get; set; }
    public double aspectY { get; set; }

    // TODO: convenience methods
    // lens (computed)
    // target point
    // forward direction
    // up direction
    [JsonIgnore] public Point cameraPosition => transform.ApplyToPoint(new Point(0, 0, 0, units));

    public View() { }
  }

  public enum ViewType
  {
    Perspective,
    Orthogonal,
    Panoramic
  }
}
