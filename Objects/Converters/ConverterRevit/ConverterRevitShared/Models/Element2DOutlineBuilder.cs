using System;
using System.Collections.Generic;
using System.Linq;
using ConverterRevitShared.Extensions;
using Objects.Geometry;

namespace ConverterRevitShared.Models;

/// <summary>
/// Responsible for taking the outermost loop of an analytical 2D element (as a list of points) and
/// adding back the sections that have been removed due to openings being present. This way, instead of sending
/// a wall with a door as 8-sided n-gon, we'll send it as a rectangle with the polyline representing the door
/// opening in a separate property. The latter format is much easier for structural software to represent and
/// is a more clear representation of the design intent.
/// </summary>
internal class Element2DOutlineBuilder
{
  private const double POINT_TOLERANCE = .1;

  private readonly List<Polyline> openingPolylines;
  private readonly List<Point> outlinePoints;

  public Element2DOutlineBuilder(List<Polyline> openingPolylines, List<Point> outlinePoints)
  {
    this.openingPolylines = openingPolylines;
    this.outlinePoints = outlinePoints;
  }

  private void AddPointsToOutline(List<Point> pointsToAdd, int indexToBeInserted)
  {
    if (outlinePoints.Count == 0)
    {
      if (indexToBeInserted > 0)
      {
        throw new ArgumentException($"Outline currently has 0 points, cannot add point at index {indexToBeInserted}");
      }
      outlinePoints.AddRange(pointsToAdd);
    }
    else
    {
      AddPointsToPopulatedOutline(pointsToAdd, indexToBeInserted);
    }
  }

  private void AddPointsToPopulatedOutline(List<Point> pointsToAdd, int insertIndex)
  {
    Point previousPoint = outlinePoints[insertIndex];

    if (previousPoint.DistanceTo(pointsToAdd.First()) < POINT_TOLERANCE)
    {
      int prevNumOrLast = insertIndex == 0 ? outlinePoints.Count - 1 : insertIndex - 1;
      RemoveLastPointToAddIfItAlreadyExists(pointsToAdd, prevNumOrLast);
      outlinePoints.InsertRange(insertIndex, pointsToAdd.Skip(1));
    }
    else if (previousPoint.DistanceTo(pointsToAdd.Last()) < POINT_TOLERANCE)
    {
      pointsToAdd.Reverse();
      int nextNumOrZero = insertIndex == outlinePoints.Count - 1 ? 0 : insertIndex + 1;
      RemoveLastPointToAddIfItAlreadyExists(pointsToAdd, nextNumOrZero);
      outlinePoints.InsertRange(insertIndex, pointsToAdd.Skip(1));
    }
    else
    {
      throw new InvalidOperationException(
        "The provided list of points that doesn't start at the beginning or end of the existing outline"
      );
    }
  }

  private void RemoveLastPointToAddIfItAlreadyExists(List<Point> pointsToAdd, int nextPointIndex)
  {
    Point nextPoint = outlinePoints[nextPointIndex];
    if (nextPoint.DistanceTo(pointsToAdd.Last()) < POINT_TOLERANCE)
    {
      pointsToAdd.RemoveAt(pointsToAdd.Count - 1);
    }
  }

  /// <summary>
  /// Will go through each opening and, if the opening is partially on the edge of an area topology,
  /// then it will alter the topology such that the section of the opening that is part of the topology
  /// is removed and the rest, the section of the opening that is not on the topology, will be added to it
  /// </summary>
  /// <returns></returns>
  public List<Point> GetOutline()
  {
    foreach (Polyline polyline in openingPolylines)
    {
      List<LineOverlappingOutlineData> lineOverlapData = polyline
        .EnumerateAsLines()
        .Select(GetLineOverlappingOutlineData)
        .ToList();

      if (
        !lineOverlapData.Any(data => data.OverlapsOutline)
        || RemoveIndicesFromOutline(lineOverlapData) is not int indexToAddTo
      )
      {
        continue;
      }

      List<Line> linesToAddToOutline = lineOverlapData
        .Where(data => !data.OverlapsOutline)
        .Select(data => data.Line)
        .ToList();

      List<Point> pointsToAddToOutline = linesToAddToOutline.Select(line => line.start).ToList();
      pointsToAddToOutline.Add(linesToAddToOutline.Last().end);

      AddPointsToOutline(pointsToAddToOutline, indexToAddTo);
    }

    RemoveRedundantPointsFromOutline();

    return outlinePoints;
  }

  void RemoveRedundantPointsFromOutline()
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

  int? RemoveIndicesFromOutline(List<LineOverlappingOutlineData> allLineOverlapData)
  {
    SortedSet<int> allIndicesInvolved = new();
    SortedSet<int> indicesToRemove = new();
    foreach (LineOverlappingOutlineData data in allLineOverlapData)
    {
      if (data.OutlineIndexForStartPoint.HasValue)
      {
        SortIndicesIntoCorrectSet(data.OutlineIndexForStartPoint.Value, allIndicesInvolved, indicesToRemove);
        SortIndicesIntoCorrectSet(data.OutlineIndexForEndPoint.Value, allIndicesInvolved, indicesToRemove);
      }
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
    // in the above scenario, only the bottom two indices of the opening will be included in the
    // allIndicesInvolved set and none will be in the indicesToRemove set. What we need to do is remove
    // the two points in the allIndicesInvolved from the outline and NOT add the missing ones like we will
    // typically do

    if (allIndicesInvolved.Count == 2 && indicesToRemove.Count == 0)
    {
      outlinePoints.RemoveAt(allIndicesInvolved.ElementAt(1));
      outlinePoints.RemoveAt(allIndicesInvolved.ElementAt(0));
      return null;
    }

    // loop backward to remove items from the list at certain indices without shifting the rest of the indices
    foreach (int indexToRemove in indicesToRemove.Reverse())
    {
      outlinePoints.RemoveAt(indexToRemove);
    }

    return indicesToRemove.Min();
  }

  static void SortIndicesIntoCorrectSet(
    int currentIndex,
    SortedSet<int> allIndicesInvolved,
    SortedSet<int> indicesToRemove
  )
  {
    if (allIndicesInvolved.Contains(currentIndex))
    {
      if (!indicesToRemove.Contains(currentIndex))
      {
        indicesToRemove.Add(currentIndex);
      }
    }
    else
    {
      allIndicesInvolved.Add(currentIndex);
    }
  }

  LineOverlappingOutlineData GetLineOverlappingOutlineData(Line openingLine)
  {
    Point previousPoint = outlinePoints[0];
    for (int i = 1; i < outlinePoints.Count; i++)
    {
      Point currentPoint = outlinePoints[i];
      if (
        openingLine.start.DistanceTo(currentPoint) < POINT_TOLERANCE
        && openingLine.end.DistanceTo(previousPoint) < POINT_TOLERANCE
      )
      {
        return new(openingLine, true, i, i - 1);
      }
      else if (
        openingLine.end.DistanceTo(currentPoint) < POINT_TOLERANCE
        && openingLine.start.DistanceTo(previousPoint) < POINT_TOLERANCE
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
