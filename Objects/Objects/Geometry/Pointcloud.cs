using System.Collections.Generic;
using System.Linq;

using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Logging;

using Objects.Other;

namespace Objects.Geometry
{
  public class Pointcloud : Base, IHasBoundingBox, ITransformable<Pointcloud>
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
    { }

    
    /// <param name="points">Flat list of x,y,z coordinates</param>
    /// <param name="colors">Optional list of colors</param>
    /// <param name="sizes">Optional list of sizes</param>
    [SchemaInfo(nameof(Pointcloud), "Create a point cloud object", "Objects", "Geometry")]
    public Pointcloud(List<double> points, List<int> colors = null, List<double> sizes = null)
    {
      this.points = points;
      this.colors = colors ?? new List<int>();
      this.sizes = sizes ?? new List<double>();

    }

    /// <returns><see cref="points"/> as list of <see cref="Point"/>s</returns>
    /// <exception cref="SpeckleException">when list is malformed</exception>
    public List<Point> GetPoints()
    {
      if (points.Count % 3 != 0) throw new SpeckleException($"{nameof(Pointcloud)}.{nameof(points)} list is malformed: expected length to be multiple of 3");
      
      var pts = new List<Point>(points.Count / 3);
      for (int i = 2; i < points.Count; i += 3)
      {
        pts.Add(new Point(points[i - 2], points[i - 1], points[i], units));
      }
      return pts;
    }

    public bool TransformTo(Transform transform, out Pointcloud pointcloud)
    {
      // transform points
      var transformedPoints = new List<Point>();
      foreach (var point in GetPoints())
      {
        point.TransformTo(transform, out Point transformedPoint);
        transformedPoints.Add(transformedPoint);
      }

      pointcloud = new Pointcloud
      {
        units = units,
        points = transformedPoints.SelectMany(o => o.ToList()).ToList(),
        colors = colors,
        sizes = sizes,
        applicationId = applicationId
      };
      
      return true;
    }

    public bool TransformTo(Transform transform, out ITransformable transformed)
    {
      var res = TransformTo(transform, out Pointcloud pc);
      transformed = pc;
      return res;
    }
  }
}
