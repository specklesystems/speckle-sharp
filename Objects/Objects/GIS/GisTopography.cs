using System.Collections.Generic;
using Objects.BuiltElements;

namespace Objects.GIS;

public class GisTopography : Topography
{
  public int band_count { get; set; }
  public List<string> band_names { get; set; }
  public float x_origin { get; set; }
  public float y_origin { get; set; }
  public int x_size { get; set; }
  public int y_size { get; set; }
  public float x_resolution { get; set; }
  public float y_resolution { get; set; }
  public List<float?> noDataValue { get; set; }
}
