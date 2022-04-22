using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements.Archicad
{
  public sealed class Floor : BuiltElements.Floor
  {
    public ElementShape shape { get; set; }

    public int? floorIndex { get; set; }

    public string structure { get; set; }

    public double? thickness { get; set; }

    public string edgeAngleType { get; set; }

    public double? edgeAngle { get; set; }

    public string referencePlaneLocation { get; set; }

    public int? compositeIndex { get; set; }

    public int? buildingMaterialIndex { get; set; }

    public Floor() { }
  }
}
