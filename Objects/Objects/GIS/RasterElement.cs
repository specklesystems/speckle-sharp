using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.GIS;

public class RasterElement : Base
{
  public int band_count { get; set; }
  public List<string>? band_names { get; set; }
  public float x_origin { get; set; }
  public float y_origin { get; set; }
  public int x_size { get; set; }
  public int y_size { get; set; }
  public float x_resolution { get; set; }
  public float y_resolution { get; set; }
  public List<float?>? noDataValue { get; set; }

  [DetachProperty]
  public List<Mesh> displayValue { get; set; }

  public RasterElement()
  {
    displayValue = new List<Mesh>();
  }

  public RasterElement(
    int bandCount,
    List<string>? bandNames,
    float xOrigin,
    float yOrigin,
    int xSize,
    int ySize,
    float xResolution,
    float yResolution,
    List<float?>? noDataValue
  )
  {
    displayValue = new List<Mesh>();
    band_count = bandCount;
    band_names = bandNames;
    x_origin = xOrigin;
    y_origin = yOrigin;
    x_size = xSize;
    y_size = ySize;
    x_resolution = xResolution;
    y_resolution = yResolution;
    this.noDataValue = noDataValue;
  }
}
