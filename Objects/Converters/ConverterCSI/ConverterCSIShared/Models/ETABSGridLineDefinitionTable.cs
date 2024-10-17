#nullable enable
using System;
using ConverterCSIShared.Services;
using CSiAPIv1;
using Objects.BuiltElements;
using Objects.Geometry;
using Objects.Other;
using Speckle.Core.Logging;

namespace ConverterCSIShared.Models;

/// <summary>
/// Encapsulates the logic dealing with creating and manipulating gridlines via the interactive database in ETABS
/// </summary>
internal class ETABSGridLineDefinitionTable : DatabaseTableWrapper
{
  private const double gridTolerance = .001; // .05 degrees as radians
  public override string TableKey => "Grid Definitions - Grid Lines";
  public static string?[] DefaultRow =>
    new string?[]
    {
      null, // Name : Grid System name
      null, // LineType
      null, // Id : Grid name
      null, // Ordinate : Offset in the positive direction
      null, // Angle : clockwise offset in degrees for polar (cylindrical) coordinates
      null, // X1
      null, // Y1
      null, // X2
      null, // Y2
      "Start", // BubbleLoc
      "Yes", // Visible
    };

  public ETABSGridLineDefinitionTable(cSapModel cSapModel, ToNativeScalingService toNativeScalingService)
    : base(cSapModel, toNativeScalingService) { }

  public const string XGridLineType = "X (Cartesian)";
  public const string YGridLineType = "Y (Cartesian)";
  public const string RGridLineType = "R (Cylindrical)";
  public const string TGridLineType = "T (Cylindrical)";

  /// <summary>
  /// Add a gridline that is represented by a straight line to the ETABS document
  /// </summary>
  /// <param name="gridSystemName"></param>
  /// <param name="gridLineType"></param>
  /// <param name="gridName"></param>
  /// <param name="location"></param>
  /// <param name="visible"></param>
  /// <exception cref="ArgumentException"></exception>
  public void AddCartesian(
    string gridSystemName,
    string gridLineType,
    string gridName,
    double location,
    string visible = "Yes"
  )
  {
    if (gridLineType != XGridLineType && gridLineType != YGridLineType)
    {
      throw new ArgumentException(
        $"Argument gridLineType must be either {XGridLineType} or {YGridLineType}",
        nameof(gridLineType)
      );
    }

    var newRow = new string?[fieldKeysIncluded.Length];
    for (var index = 0; index < fieldKeysIncluded.Length; index++)
    {
      newRow[index] = fieldKeysIncluded[index] switch
      {
        "Name" => gridSystemName,
        "LineType" => gridLineType,
        "ID" => gridName,
        "Ordinate" => location.ToString(),
        "Visible" => visible,
        _ => DefaultRow[index],
      };
    }

    AddRowToBeCommitted(newRow);
  }

  /// <summary>
  /// Add a gridline that is represented by a straight line to the ETABS document
  /// </summary>
  /// <param name="gridLine"></param>
  /// <exception cref="ArgumentException"></exception>
  /// <exception cref="SpeckleException"></exception>
  public void AddCartesian(GridLine gridLine)
  {
    if (gridLine.baseLine is not Line line)
    {
      throw new ArgumentException("Non line based gridlines are not supported", nameof(gridLine));
    }

    var gridRotation = GetAngleOffsetFromGlobalCoordinateSystem(line);

    var gridSystem = GetExistingGridSystem(gridRotation) ?? CreateGridSystem(gridRotation);
    var transform = GetTransformFromGridSystem(gridSystem);
    _ = line.TransformTo(transform.Inverse(), out Line transformedLine);

    var newUx = Math.Abs(transformedLine.start.x - transformedLine.end.x);
    var newUy = Math.Abs(transformedLine.start.y - transformedLine.end.y);

    string lineType;
    double gridLineOffset;
    if (newUx < gridTolerance)
    {
      lineType = XGridLineType;
      gridLineOffset = toNativeScalingService.ScaleLength(
        transformedLine.start.x,
        transformedLine.units ?? transformedLine.start.units
      );
    }
    else if (newUy < gridTolerance)
    {
      lineType = YGridLineType;
      gridLineOffset = toNativeScalingService.ScaleLength(
        transformedLine.start.y,
        transformedLine.units ?? transformedLine.start.units
      );
    }
    else
    {
      throw new SpeckleException(
        $"Error in transforming line from global coordinates to grid system with rotation {gridSystem.Rotation} and x,y offsets {gridSystem.XOrigin}, {gridSystem.YOrigin}"
      );
    }

    AddCartesian(gridSystem.Name, lineType, gridLine.label, gridLineOffset);
  }

  /// <summary>
  /// Returns the rotation counter-clockwise from from the global x axis in radians
  /// </summary>
  /// <param name="line"></param>
  /// <returns></returns>
  private static double GetAngleOffsetFromGlobalCoordinateSystem(Line line)
  {
    var ux = line.start.x - line.end.x;
    var uy = line.start.y - line.end.y;

    if (Math.Abs(ux) < gridTolerance)
    {
      return Math.PI / 2;
    }
    return Math.Atan(uy / ux);
  }

  /// <summary>
  /// Find a GridSystem in the CSi model whose local x axis is either parallel or perpendicular to the provided
  /// grid angle.
  /// </summary>
  /// <param name="gridRotation">Rotation counter-clockwise from the global x axis in radians</param>
  /// <returns></returns>
  private GridSystemRepresentation? GetExistingGridSystem(double gridRotation)
  {
    var numGridSys = 0;
    var gridSysNames = Array.Empty<string>();
    this.cSapModel.GridSys.GetNameList(ref numGridSys, ref gridSysNames);

    var xOrigin = 0.0;
    var yOrigin = 0.0;
    var rotationDeg = 0.0;
    foreach (var gridSysName in gridSysNames)
    {
      var success = cSapModel.GridSys.GetGridSys(gridSysName, ref xOrigin, ref yOrigin, ref rotationDeg);
      if (success != 0)
      {
        // something went wrong. This is not necessarily unexpected or bad
        continue;
      }

      var rotationRad = rotationDeg * Math.PI / 180;
      var combinedRotationsNormalized = Math.Abs((rotationRad - gridRotation) / (Math.PI / 2));
      var combinedRotationsRemainder = combinedRotationsNormalized - Math.Floor(combinedRotationsNormalized);

      if (combinedRotationsRemainder < gridTolerance || combinedRotationsRemainder > 1 - gridTolerance)
      {
        return new GridSystemRepresentation(gridSysName, xOrigin, yOrigin, rotationRad);
      }
    }
    // could not find compatible existing grid system
    return null;
  }

  private GridSystemRepresentation CreateGridSystem(double gridRotation)
  {
    var systemName = GetUniqueGridSystemName();
    _ = cSapModel.GridSys.SetGridSys(systemName, 0, 0, gridRotation * 180 / Math.PI);

    return new GridSystemRepresentation(systemName, 0, 0, gridRotation);
  }

  /// <summary>
  /// Returns a unique name to be used for a new speckle-created grid system.
  /// </summary>
  /// <returns></returns>
  private string GetUniqueGridSystemName()
  {
    var numGridSys = 0;
    var gridSysNames = Array.Empty<string>();
    this.cSapModel.GridSys.GetNameList(ref numGridSys, ref gridSysNames);

    var numberOfGridSystems = 0;
    var gridSystemNamePrefix = "SpeckleGridSystem";
    foreach (var gridSysName in gridSysNames)
    {
      // test if this grid system is one that we already created. If it is, then we need to adjust our
      // numberOfGridSystems so that if we do end up creating a new one, it doesn't override an existing one.
      if (!gridSysName.StartsWith(gridSystemNamePrefix))
      {
        continue;
      }

      if (int.TryParse(gridSysName.Replace(gridSystemNamePrefix, ""), out int gridSysNum))
      {
        numberOfGridSystems = Math.Max(numberOfGridSystems, gridSysNum + 1);
      }
    }
    return $"{gridSystemNamePrefix}{numberOfGridSystems}";
    ;
  }

  private static Transform GetTransformFromGridSystem(GridSystemRepresentation sys)
  {
    return new Transform(
      new double[]
      {
        Math.Cos(sys.Rotation),
        -Math.Sin(sys.Rotation),
        0,
        sys.XOrigin,
        Math.Sin(sys.Rotation),
        Math.Cos(sys.Rotation),
        0,
        sys.YOrigin,
        0,
        0,
        1,
        0,
        0,
        0,
        0,
        1
      }
    );
  }
}

public class GridSystemRepresentation
{
  public GridSystemRepresentation(string name, double xOrigin, double yOrigin, double rotation)
  {
    Name = name;
    XOrigin = xOrigin;
    YOrigin = yOrigin;
    Rotation = rotation;
  }

  public string Name { get; }
  public double XOrigin { get; set; }
  public double YOrigin { get; set; }
  public double Rotation { get; set; }
}
