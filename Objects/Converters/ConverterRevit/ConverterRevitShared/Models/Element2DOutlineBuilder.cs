using System;
using System.Collections.Generic;
using System.Linq;
using ConverterRevitShared.Extensions;
using Objects;
using Objects.BuiltElements;
using Objects.Geometry;
using Speckle.Core.Logging;

namespace ConverterRevitShared.Models
{
  internal class Element2DOutlineBuilder
  {
    private const double pointTolerance = .1;

    private readonly List<Polyline> openingPolylines;
    private readonly List<Point> outlinePoints;

    public Element2DOutlineBuilder(List<Polyline> openingPolylines, List<Point> outlinePoints)
    {
      this.openingPolylines = openingPolylines;
      this.outlinePoints = outlinePoints;
    }

    void AddPointsToOutline(List<Point> pointsToAdd, int? indexToBeInserted = null)
    {
      if (outlinePoints.Count == 0)
      {
        if (indexToBeInserted == null || indexToBeInserted.Value == 0)
        {
          outlinePoints.AddRange(pointsToAdd);
        }
        throw new ArgumentException($"Outline current has 0 points, cannot add point at index {indexToBeInserted}");
      }

      int insertIndex = indexToBeInserted == null ? outlinePoints.Count : indexToBeInserted.Value;
      Point previousPoint = outlinePoints[insertIndex];

      if (previousPoint.DistanceTo(pointsToAdd.First()) < pointTolerance)
      {
        int prevNumOrLast = insertIndex == 0 ? outlinePoints.Count - 1 : insertIndex - 1;
        Point nextPoint = outlinePoints[prevNumOrLast];

        if (nextPoint.DistanceTo(pointsToAdd.Last()) < pointTolerance)
        {
          pointsToAdd.RemoveAt(pointsToAdd.Count - 1);
        }
        outlinePoints.InsertRange(indexToBeInserted.Value, pointsToAdd.Skip(1));
      }
      else if (previousPoint.DistanceTo(pointsToAdd.Last()) < pointTolerance)
      {
        pointsToAdd.Reverse();
        int nextNumOrZero = insertIndex == outlinePoints.Count - 1 ? 0 : insertIndex + 1;
        Point nextPoint = outlinePoints[nextNumOrZero];

        if (nextPoint.DistanceTo(pointsToAdd.Last()) < pointTolerance)
        {
          pointsToAdd.RemoveAt(pointsToAdd.Count - 1);
        }
        outlinePoints.InsertRange(indexToBeInserted.Value, pointsToAdd.Skip(1));
      }
    }

    public List<Point> GetOutline()
    {
      //foreach (Polyline polyline in openingPolylines)
      //{
      //  List<LineOverlappingOutlineData> lineOverlapData = EnumeratePolyline(polyline)
      //    .Select(GetLineOverlappingOutlineData)
      //    .ToList();

      //  if (!lineOverlapData.Any(data => data.OverlapsOutline))
      //  {
      //    continue;
      //  }

      //  //if (lineOverlapData.Any(data => data.OutlineIndexForStartPoint > data.OutlineIndexForEndPoint))
      //  //{
      //  //  lineOverlapData.Reverse();
      //  //  lineOverlapData = lineOverlapData
      //  //    .Select(data => new LineOverlappingOutlineData(
      //  //      new Line(data.Line.end, data.Line.start, data.Line.units),
      //  //      data.OverlapsOutline,
      //  //      data.OutlineIndexForEndPoint,
      //  //      data.OutlineIndexForStartPoint
      //  //    ))
      //  //    .ToList();
      //  //}

      //  int? indexToAddTo = RemoveIndiciesFromOutline(lineOverlapData);
      //  if (!indexToAddTo.HasValue)
      //  {
      //    continue;
      //  }

      //  List<Line> linesToAddToOutline = lineOverlapData
      //    .Where(data => !data.OverlapsOutline)
      //    .Select(data => data.Line)
      //    .ToList();

      //  List<Point> pointsToAddToOutline = linesToAddToOutline
      //    .Select(line => line.start)
      //    .ToList();
      //  pointsToAddToOutline.Add(linesToAddToOutline.Last().end);

      //  AddPointsToOutline(pointsToAddToOutline, indexToAddTo);
      //}

      //CleanOutline();

      return outlinePoints;
    }

    void CleanOutline()
    {
      Point previousPoint = outlinePoints[0];
      for (int i = outlinePoints.Count - 1; i >= 0; i--)
      {
        Point currentPoint = outlinePoints[i];

        int nextPointIndex = i == 0 ? outlinePoints.Count - 1 : i - 1;
        Point nextPoint = outlinePoints[nextPointIndex];

        if (currentPoint.IsOnLineBetweenPoints(previousPoint, nextPoint))
        {
          outlinePoints.RemoveAt(i);
        }
        previousPoint = currentPoint;
      }
    }
    
    int? RemoveIndiciesFromOutline(List<LineOverlappingOutlineData> allLineOverlapData)
    {
      SortedSet<int> allIndiciesInvolved = new();
      SortedSet<int> indiciesToRemove = new();
      foreach (LineOverlappingOutlineData data in allLineOverlapData)
      {
        if (!data.OutlineIndexForStartPoint.HasValue)
        {
          continue;
        }
        SortIndiciesIntoCorrectSet(data.OutlineIndexForStartPoint.Value, allIndiciesInvolved, indiciesToRemove);
        SortIndiciesIntoCorrectSet(data.OutlineIndexForEndPoint.Value, allIndiciesInvolved, indiciesToRemove);
      }

      // This code handles an edge case where there is an opening near the top of a wall.
      // the outline of the wall may dip down to include the opening, even if the opening doesn't actually reach
      // the top of the wall. For example, if you have a duct that goes through the a wall near the top, it may
      // not reach the top, but you still wouldn't need to close the wall over the top of the duct.
      //  ___                 ___________________________
      // |   |  not opening  |                           |
      // |   | but not wall  |                           |
      // |   |_______________|          wall             | 
      // |   |    opening    |                           |
      // |   |_______________|                           |
      // |                                               |
      // |                                               |
      // |_______________________________________________|
      // in the above scenario, only the bottom two indicies of the opening will be included in the
      // allIndiciesInvolved set and none will be in the indiciesToRemove set. What we need to do is remove
      // the two points in the allIndiciesInvolved from the outline and NOT add the missing ones like we will
      // typically do

      if (allIndiciesInvolved.Count == 2 && indiciesToRemove.Count == 0)
      {
        outlinePoints.RemoveAt(allIndiciesInvolved.ElementAt(1));
        outlinePoints.RemoveAt(allIndiciesInvolved.ElementAt(0));
        return null;
      }

      // loop backward to remove items from the list at certain indicies without shifting the rest of the indicies
      foreach (int indexToRemove in indiciesToRemove.Reverse())
      {
        outlinePoints.RemoveAt(indexToRemove);
      }

      return indiciesToRemove.Min();
    }

    static void SortIndiciesIntoCorrectSet(int currentIndex, SortedSet<int> allIndiciesInvolved, SortedSet<int> indiciesToRemove)
    {
      if (allIndiciesInvolved.Contains(currentIndex))
      {
        if (!indiciesToRemove.Contains(currentIndex))
        {
          indiciesToRemove.Add(currentIndex);
        }
      }
      else
      {
        allIndiciesInvolved.Add(currentIndex);
      }
    }

    static IEnumerable<Line> EnumeratePolyline(Polyline polyline)
    {
      List<Point> points = polyline.GetPoints();
      Point previousPoint = points[0];
      for (int i = 1; i < points.Count; i++)
      {
        yield return new Line(previousPoint, points[i], polyline.units);
        previousPoint = points[i];
      }
    }

    LineOverlappingOutlineData GetLineOverlappingOutlineData(Line openingLine)
    {
      Point previousPoint = outlinePoints[0];
      for (int i = 1; i < outlinePoints.Count; i++)
      {
        Point currentPoint = outlinePoints[i];
        if (
          openingLine.start.DistanceTo(currentPoint) < pointTolerance
          && openingLine.end.DistanceTo(previousPoint) < pointTolerance
        )
        {
          return new(openingLine, true, i, i - 1);
        }
        else if (
          openingLine.end.DistanceTo(currentPoint) < pointTolerance
          && openingLine.start.DistanceTo(previousPoint) < pointTolerance
        )
        {
          return new(openingLine, true, i - 1, i);
        }
        previousPoint = currentPoint;
      }
      return new LineOverlappingOutlineData(openingLine, false);
    }
  }

  public readonly struct LineOverlappingOutlineData
  {
    public LineOverlappingOutlineData(
      Line line, 
      bool overlapsOutline,
      int? outlineIndexForStartPoint = null,
      int? outlineIndexForEndPoint = null
    )
    {
      OverlapsOutline = overlapsOutline;
      this.OutlineIndexForStartPoint = outlineIndexForStartPoint;
      this.OutlineIndexForEndPoint = outlineIndexForEndPoint;
      Line = line;
    }

    public Line Line { get; }
    public bool OverlapsOutline { get; }
    public int? OutlineIndexForStartPoint { get; }
    public int? OutlineIndexForEndPoint { get; }
  }
}
