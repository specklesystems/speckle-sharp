using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.Interpolation;
using MathNet.Spatial.Euclidean;

namespace StructuralUtilities.PolygonMesher
{
  public class PolygonMesher
  {
    private CoordinateSystem CoordinateTranslation = null;

    private readonly ClosedLoop ExternalLoop = new ClosedLoop();
    private readonly List<ClosedLoop> Openings = new List<ClosedLoop>();

    private readonly Dictionary<IndexPair, Line2D> Internals = new Dictionary<IndexPair, Line2D>();
    private readonly List<TriangleIndexSet> Triangles = new List<TriangleIndexSet>();

    private readonly double tolerance = 0.001;

    public static double PointComparisonEpsilon = 0.0001;

    public double[] Coordinates
    {
      get
      {
        var coord = new List<double>();
        var vs = GetAllVertices();
        foreach (var v in vs)
        {
          coord.AddRange(v.Coordinates);
        }
        return coord.ToArray();
      }
    }

    //Assumptions:
    // - input coordinates are an ordered set of external vertices (i.e. not vertices of openings)
    // - the lines defined by these vertices don't intersect each other
    public bool Init(IEnumerable<double> coords, IEnumerable<IEnumerable<double>> openingCoordsList = null)
    {
      // if there are less than 3 coordinates, we cannot create a mesh
      if (coords.Count() < 9) 
        return false;

      ExternalLoop.Init(coords, ref CoordinateTranslation, tolerance);

      if (openingCoordsList != null)
      {
        var indexOffset = ExternalLoop.Vertices.Count();
        foreach (var openingGlobalCoords in openingCoordsList)
        {
          var openingLoop = new ClosedLoop();
          openingLoop.Init(openingGlobalCoords, ref this.CoordinateTranslation, tolerance, indexOffset);
          //openings are inverse loops - the inside should be considered the outside, to reverse the winding order
          openingLoop.ReverseDirection();
          indexOffset += openingLoop.Vertices.Count();
          Openings.Add(openingLoop);
        }
      }

      if (!GenerateInternals())
      {
        return false;
      }

      if (!GenerateFaces())
      {
        return false;
      }

      return true;
    }

    private bool GenerateInternals()
    {
      var loops = GetLoops();

      //Go through each loop
      foreach (var l in loops)
      {
        for (var i = l.FirstIndex(); i <= l.LastIndex(); i++)
        {
          var nextPtIndex = l.NextIndex(i);
          var prevPtIndex = l.PrevIndex(i);
          var dictHalfSweep = PointIndicesSweep(i, nextPtIndex, prevPtIndex, l.WindingDirection);

          if (dictHalfSweep == null)
          {
            //This would only happen if winding direction hasn't been determined yet
            return false;
          }

          if (dictHalfSweep.Keys.Count > 0)
          {
            //Go through each direction (ending in an external point) emanating from this candidate point
            foreach (var vector in dictHalfSweep.Keys)
            {
              double? currShortestDistance = null;
              int? shortestPtIndex = null;

              //Go through each point in the same direction from this candidate point (because multiple external points can be in alignment
              //along the same direction)
              foreach (var ptIndex in dictHalfSweep[vector])
              {
                var indexPair = new IndexPair(i, ptIndex);
                var line = GetLine(indexPair);
                if (!ValidNewInternalLine(indexPair, line)) continue;

                //Calculate distance - if currShortestDistance is either -1 or shorter than the shortest, replace the shortest
                var distance = line.Length;
                if (!currShortestDistance.HasValue || (distance < currShortestDistance))
                {
                  currShortestDistance = distance;
                  shortestPtIndex = ptIndex;
                }
              }

              //Now that the shortest valid line to another point has been found, add it to the list of lines
              if (currShortestDistance > 0 && shortestPtIndex.HasValue && shortestPtIndex > 0)
              {
                //Add line - which has already been checked to ensure it doesn't intersect others, etc
                var shortestIndexPair = new IndexPair(i, shortestPtIndex.Value);
                Internals.Add(shortestIndexPair, GetLine(shortestIndexPair));
                continue;
              }
            }
          }
        }
      }

      return true;
    }

    //This method might become useful when visualising these in Rhino, for example
    public List<double[]> GetInternalGlobalCoords()
    {
      var l = new List<double[]>();
      var indexPoints = AllGlobalPoints;

      foreach (var internalPair in Internals.Keys)
      {
        var startPt = indexPoints[internalPair.Indices[0]];
        var endPt = indexPoints[internalPair.Indices[1]];
        l.Add(new double[] { startPt.X, startPt.Y, startPt.Z, endPt.X, endPt.Y, endPt.Z });
      }

      return l;
    }
        
    public List<int> Faces(int faceIndexOffset = 0)
    {
      var faces = new List<int>();
      foreach (var t in Triangles)
      {
        faces.Add(3); // signifying a triangle
        faces.AddRange(t.Indices.Take(3).Select(x => x + faceIndexOffset));
      }

      return faces;
    }

    private bool GenerateFaces()
    {
      //Now determine faces by cycling through each edge line and finding which other point is shared between all lines emanating from this point
      Triangles.Clear();

      var indexPairs = GetAllIndexPairs();
      var vertices = GetAllVertices();
      var vertexSets = new List<int[]>();

      foreach (var vertex in vertices)
      {
        var linkedIndices = indexPairs.Where(v => v.Contains(vertex.Index)).Select(v => v.Other(vertex.Index).Value).ToList();

        foreach (var l1i in linkedIndices)
        {
          var level2Indices = indexPairs.Where(v => v.Contains(l1i) && !v.Contains(vertex.Index)).Select(v => v.Other(l1i).Value).ToList();
          foreach (var l2i in level2Indices)
          {
            if (indexPairs.Any(ip => ip.Matches(new IndexPair(l2i, vertex.Index))))
            {
              var triangle = new TriangleIndexSet(vertex.Index, l1i, l2i);
              if (Triangles.All(t => !t.Matches(triangle)))
              {
                Triangles.Add(triangle);
              }
            }
          }
        }
      }

      return true;
    }

    private Dictionary<IndexPair, Line2D> AllIndexLocalLines
    {
      get
      {
        var pairs = new Dictionary<IndexPair, Line2D>();
        foreach (var k in Internals.Keys)
        {
          pairs.Add(k, Internals[k]);
        }
        foreach (var k in ExternalLoop.IndexedLines.Keys)
        {
          pairs.Add(k, ExternalLoop.IndexedLines[k]);
        }

        foreach (var l in Openings)
        {
          foreach (var k in l.IndexedLines.Keys)
          {
            pairs.Add(k, l.IndexedLines[k]);
          }
        }
        return pairs;
      }
    }

    private Dictionary<int, Point3D> AllGlobalPoints
    {
      get
      {
        var indexPoints = new Dictionary<int, Point3D>();

        foreach (var l in GetLoops())
        {
          foreach (var mp in l.Vertices)
          {
            indexPoints.Add(mp.Index, mp.Global);
          }
        }
        return indexPoints;
      }
    }

    private bool ExistingLinesContains(IndexPair indexPair)
    {
      var matching = AllIndexLocalLines.Keys.Where(i => i.Matches(indexPair));
      return (matching.Count() > 0);
    }

    private Line2D GetLine(IndexPair indexPair)
    {
      var allPts = GetAllPts();
      return new Line2D(allPts[indexPair.Indices[0]], allPts[indexPair.Indices[1]]);
    }

    private List<ClosedLoop> GetLoops()
    {
      var l = new List<ClosedLoop>() { ExternalLoop };
      if (Openings != null && Openings.Count() > 0)
      {
        l.AddRange(Openings);
      }
      return l;
    }

    private bool ValidNewInternalLine(IndexPair indexPair, Line2D line)
    {
      if (indexPair == null) return false;

      //Check if this line is already in the collection - if so, ignore it
      if (ExistingLinesContains(indexPair)) return false;

      //Check if this line would intersect any external lines in this collection - if so, ignore it
      if (IntersectsBoundaryLines(line)) return false;

      //Check if this line would intersect any already in this collection - if so, ignore it
      if (IntersectsInternalLines(line)) return false;

      return true;
    }

    private List<int> GetPairedIndices(int index)
    {
      return AllIndexLocalLines.Keys.Select(l => l.Other(index)).Where(l => l.HasValue).Cast<int>().ToList();
    }

    private List<Point2D> GetAllPts()
    {
      var allPts = new List<Point2D>();
      allPts.AddRange(ExternalLoop.Vertices.Select(mp => mp.Local));
      if (Openings != null)
      {
        foreach (var opening in Openings)
        {
          allPts.AddRange(opening.Vertices.Select(p => p.Local));
        }
      }
      return allPts;
    }

    private List<Vertex> GetAllVertices()
    {
      return GetLoops().SelectMany(l => l.Vertices).ToList();
    }

    private List<Line2D> GetAllBoundaryLines()
    {
      var allBoundaryLines = new List<Line2D>();
      allBoundaryLines.AddRange(ExternalLoop.IndexedLines.Select(p => p.Value));
      foreach (var opening in Openings)
      {
        allBoundaryLines.AddRange(opening.IndexedLines.Select(p => p.Value));
      }
      return allBoundaryLines;
    }

    private List<IndexPair> GetAllBoundaryIndexPairs()
    {
      var allBoundaryPairs = new List<IndexPair>();
      allBoundaryPairs.AddRange(ExternalLoop.IndexedLines.Select(p => p.Key));
      foreach (var opening in Openings)
      {
        allBoundaryPairs.AddRange(opening.IndexedLines.Select(p => p.Key));
      }
      return allBoundaryPairs;
    }

    private List<IndexPair> GetAllIndexPairs()
    {
      var allPairs = new List<IndexPair>();
      allPairs.AddRange(GetAllBoundaryIndexPairs());
      allPairs.AddRange(Internals.Select(i => i.Key));
      return allPairs;
    }

    private bool IntersectsBoundaryLines(Line2D line)
    {
      var allBoundaryLines = GetAllBoundaryLines();
      foreach (var bl in allBoundaryLines)
      {
        if (bl.Intersects(line, tolerance))
        {
          return true;
        }
      }

      return false;
    }

    private bool IntersectsInternalLines(Line2D line)
    {
      foreach (var ik in Internals.Keys)
      {
        if (Internals[ik].Intersects(line, tolerance))
        {
          return true;
        }
      }

      return false;
    }

    //Because multiple points can be aligned along the same direction from any given point, a dictionary is returned where
    //the (unit) vectors towards the points are the keys, and all points in that exact direction listed as the values
    private Dictionary<Vector2D, List<int>> PointIndicesSweep(int ptIndex, int nextPtIndex, int prevPtIndex, int windingDirection)
    {
      if (windingDirection == 0)
      {
        return null;
      }

      var allPts = GetAllPts();

      var vCurrToNext = allPts[ptIndex].VectorTo(allPts[nextPtIndex]).Normalize();
      var vCurrToPrev = allPts[ptIndex].VectorTo(allPts[prevPtIndex]).Normalize();

      var dict = new Dictionary<Vector2D, List<int>>();

      for (var i = 0; i < allPts.Count(); i++)
      {
        if (i == ptIndex || i == nextPtIndex || i == prevPtIndex) continue;

        var vItem = allPts[ptIndex].VectorTo(allPts[i]).Normalize();

        //The swapping of the vectors below is to align with the fact that the vector angle comparison is always done anti-clockwise
        var isBetween = (windingDirection > 0) ? vItem.IsBetweenVectors(vCurrToNext, vCurrToPrev) : vItem.IsBetweenVectors(vCurrToPrev, vCurrToNext);

        if (isBetween)
        {
          if (!dict.ContainsKey(vItem))
          {
            dict.Add(vItem, new List<int>());
          }
          dict[vItem].Add(i);
        }
      }
      return dict;
    }
  }
}
