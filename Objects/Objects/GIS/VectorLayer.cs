using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Objects.BuiltElements;
using Objects.Structural.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.GIS;

public class VectorLayer : Collection
{
  public CRS? crs { get; set; }
  public string? units { get; set; }
  public Base? attributes { get; set; }
  public string? geomType { get; set; }
  public Dictionary<string, object>? renderer { get; set; }

  public VectorLayer()
  {
    collectionType = "VectorLayer";
  }
}
