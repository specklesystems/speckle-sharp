using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.Geometry
{
  public class Pointcloud : Base, IHasBoundingBox
  {

    [DetachProperty]
    [Chunkable(31250)]
    public List<double> points { get; set; } = new List<double>();

    [DetachProperty]
    [Chunkable(62500)]
    public List<int> colors { get; set; } = new List<int>();

    [DetachProperty]
    [Chunkable(62500)]
    public List<double> sizes { get; set; } = new List<double>();

    public Box bbox { get; set; }

    public string units { get; set; }

    public Pointcloud()
    {
    }

    public IEnumerable<Point> GetPoints()
    {
      if (points.Count % 3 != 0) throw new Speckle.Core.Logging.SpeckleException("Array malformed: length%3 != 0.");

      Point[] pts = new Point[points.Count / 3];
      var asArray = points.ToArray();
      for (int i = 2, k = 0; i < points.Count; i += 3)
        pts[k++] = new Point(asArray[i - 2], asArray[i - 1], asArray[i], units);
      return pts;
    }
  }
}