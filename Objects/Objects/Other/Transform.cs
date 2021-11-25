using System.Collections;
using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.Other
{
  /// <summary>
  /// The 4x4 transform matrix.
  /// </summary>
  /// <remarks>
  /// The 3x3 sub-matrix determines scaling.
  /// The 4th column defines translation, where the last value could be a divisor.
  /// </remarks>
  public class Transform : Base
  {
    public double[ ] value { get; set; } = {1.0, 0, 0, 0, 0, 1.0, 0, 0, 0, 0, 1.0, 0, 0, 0, 0, 1.0};

    [JsonIgnore] public double[ ] translation => value.Subset(3, 7, 11, 15);

    [JsonIgnore] public double[ ] scaling => value.Subset(0, 1, 2, 4, 5, 6, 8, 9, 10);

    [JsonIgnore] public bool isIdentity => value == new[ ] {1.0, 0, 0, 0, 0, 1.0, 0, 0, 0, 0, 1.0, 0, 0, 0, 0, 1.0};

    [JsonIgnore] public bool isScaled => scaling != new[ ] {1.0, 0, 0, 0, 1.0, 0, 0, 0, 1.0};

    public Transform()
    {
    }

    public Transform(double[ ] value)
    {
      this.value = value;
    }

    // TODO! Apply to vector

    /// <summary>
    /// Transform a flat list of doubles representing points
    /// </summary>
    public List<double> ApplyToPoints(List<double> points)
    {
      var transformed = new List<double>(points.Count);
      for ( var i = 0; i < points.Count; i += 3 )
        transformed.AddRange(ApplyToPoint(new List<double>(3) {points[ i ], points[ i + 1 ], points[ i + 2 ]}));

      return transformed;
    }

    /// <summary>
    /// Transform a flat list of speckle Points
    /// </summary>
    public List<Point> ApplyToPoints(List<Point> points)
    {
      var transformed = new List<Point>(points.Count);
      for ( var i = 0; i < points.Count; i++ )
        transformed.Add(ApplyToPoint(points[ i ]));

      return transformed;
    }


    /// <summary>
    /// Transform a single speckle Point
    /// </summary>
    public Point ApplyToPoint(Point point)
    {
      var (x, y, z, units) = point;
      var newCoords = ApplyToPoint(new List<double> {x, y, z});
      var newPoint = Point.FromList(newCoords, units);
      return newPoint;
    }

    /// <summary>
    /// Transform a list of three doubles representing a point
    /// </summary>
    public List<double> ApplyToPoint(List<double> point)
    {
      var newPoint = new List<double>(4) {point[ 0 ], point[ 1 ], point[ 2 ], 1};
      for ( var i = 0; i < 16; i += 4 )
        newPoint[ i / 4 ] = newPoint[ 0 ] * value[ i ] + newPoint[ 1 ] * value[ i + 1 ] +
                            newPoint[ 2 ] * value[ i + 2 ] + newPoint[ 3 ] * value[ i + 3 ];

      return new List<double>(3)
        {newPoint[ 0 ] / newPoint[ 3 ], newPoint[ 1 ] / newPoint[ 3 ], newPoint[ 2 ] / newPoint[ 3 ]};
    }
  }

  static class ArrayUtils
  {
    // create a subset from a specific list of indices
    public static T[ ] Subset<T>(this T[ ] array, params int[ ] indices)
    {
      var subset = new T[ indices.Length ];
      for ( var i = 0; i < indices.Length; i++ )
      {
        subset[ i ] = array[ indices[ i ] ];
      }

      return subset;
    }
  }
}