using System.Collections.Generic;
using Speckle.Core.Models;

namespace Objects.GIS;

public class VectorLayer : Collection
{
  public CRS? crs { get; set; }
  public string? units { get; set; }
  public Base attributes { get; set; }
  public string? geomType { get; set; }
  public Dictionary<string, object>? renderer { get; set; }

  public VectorLayer()
  {
    collectionType = "VectorLayer";
    attributes = new Base();
  }
}
