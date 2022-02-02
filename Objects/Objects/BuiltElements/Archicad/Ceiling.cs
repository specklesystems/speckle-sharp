using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Archicad
{
  public sealed class Ceiling : BuiltElements.Ceiling, IArchicadElementBaseData
  {
    public ElementShape shape { get; set; }

    public string elementId { get; set; } = string.Empty;

    public int? floorIndex { get; set; }

    public string structure { get; set; }

    public double? thickness { get; set; }

    public string edgeAngleType { get; set; }

    public double? edgeAngle { get; set; }

    public string referencePlaneLocation { get; set; }

    public int? compositeIndex { get; set; }

    public int? buildingMaterialIndex { get; set; }

    [DetachProperty]
    public List<Mesh> displayValue { get; set; }

    public Ceiling() { }
  }
}
