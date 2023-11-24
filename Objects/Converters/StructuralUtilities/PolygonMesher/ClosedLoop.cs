using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Spatial.Euclidean;

namespace StructuralUtilities.PolygonMesher;

internal class ClosedLoop
{
  public List<Vertex> Vertices = new();
  public Dictionary<IndexPair, Line2D> IndexedLines = new();
  public int WindingDirection;
  public CoordinateSystem CoordinateTranslation;

  public bool Init(
    IEnumerable<double> globalCoords,
    ref CoordinateSystem CoordinateTranslation,
    double tolerance,
    int ptIndexOffset = 0
  )
  {
    var essential = globalCoords.Essential();

    var origPts = new List<Point3D>();
    for (var i = 0; i < essential.Length; i += 3)
    {
      origPts.Add(new Point3D(essential[i], essential[i + 1], essential[i + 2]));
    }

    if (CoordinateTranslation == null)
    {
      //Create plane
      var plane = Plane.FromPoints(origPts[0], origPts[1], origPts[2]);
      var normal = plane.Normal;
      var origin = origPts[0];
      var xDir = origPts[0].VectorTo(origPts[1]).Normalize();
      var yDir = normal.CrossProduct(xDir);

      //The CoordinateSystem class in MathNet.Spatial and its methods aren't not very intuitive as discussed in https://github.com/mathnet/mathnet-spatial/issues/53
      //Since I don't understand the offsets that seem to be applied by TransformFrom and TransformTo, I focussed on the Transform method,
      //which transforms a local point to a global point.
      //In order to transform a point from global into local, the coordinate system needs to be reversed so that the resulting coordinateSystem.Transform does the
      //transformation from global to local.
      CoordinateTranslation = new CoordinateSystem(new CoordinateSystem(origin, xDir, yDir, normal).Inverse());
    }
    else
    {
      this.CoordinateTranslation = CoordinateTranslation;
    }

    //project points onto the plane - if the points are co-planar and translation is done correctly, all Z values should be zero
    var nonCoPlanarPts = 0;
    var n = origPts.Count();
    for (var i = 0; i < n; i++)
    {
      var projectedPt = CoordinateTranslation.Transform(origPts[i]);
      if (Math.Abs(projectedPt.Z) > tolerance)
      {
        nonCoPlanarPts++;
      }
      var localPt = new Point2D(projectedPt.X, projectedPt.Y);
      Vertices.Add(new Vertex(ptIndexOffset + i, localPt, origPts[i]));
    }

    WindingDirection = 0;
    IndexedLines = new Dictionary<IndexPair, Line2D>();
    if (nonCoPlanarPts > 0)
    {
      return false;
    }

    for (var i = 0; i < n; i++)
    {
      var indexPair = new IndexPair(ptIndexOffset + i, ptIndexOffset + (i == n - 1 ? 0 : i + 1));
      IndexedLines.Add(
        indexPair,
        new Line2D(MeshPointByIndex(indexPair.Indices[0]).Local, MeshPointByIndex(indexPair.Indices[1]).Local)
      );
    }

    WindingDirection = Vertices.Select(mp => mp.Local).GetWindingDirection();

    return true;
  }

  public int NextIndex(int currIndex) => currIndex == Vertices.Last().Index ? Vertices.First().Index : currIndex + 1;

  public int PrevIndex(int currIndex) => currIndex == Vertices.First().Index ? Vertices.Last().Index : currIndex - 1;

  public int FirstIndex() => Vertices.First().Index;

  public int LastIndex() => Vertices.Last().Index;

  public void ReverseDirection()
  {
    WindingDirection *= -1;
  }

  private Vertex MeshPointByIndex(int index) => Vertices.FirstOrDefault(mp => mp.Index == index);
}
