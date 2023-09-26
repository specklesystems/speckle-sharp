using System;
using System.Linq;
using ConverterCSIShared.Services;
using CSiAPIv1;
using Objects.BuiltElements;
using Objects.Geometry;
using Objects.Other;
using Speckle.Core.Logging;

namespace ConverterCSIShared.Models
{
  internal class ETABSGridLineDefinitionTable : DatabaseTableWrapper
  {
    private int numberOfGridSystems;
    public override string TableKey => "Grid Definitions - Grid Lines";
    public string[] DefaultRow => new string[] { 
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
        throw new ArgumentException($"Argument gridLineType must be either {XGridLineType} or {YGridLineType}");
      }
      var newRow = new string[fieldKeysIncluded.Length];
      for (var index = 0; index < fieldKeysIncluded.Length; index++)
      {
        var fieldKey = fieldKeysIncluded[index];
        if (fieldKey == "Name")
        {
          newRow[index] = gridSystemName;
        }
        else if (fieldKey == "LineType")
        {
          newRow[index] = gridLineType;
        }
        else if (fieldKey == "ID")
        {
          newRow[index] = gridName;
        }
        else if (fieldKey == "Ordinate")
        {
          newRow[index] = location.ToString();
        }
        else if (fieldKey == "Visible")
        {
          newRow[index] = visible;
        }
        else
        {
          newRow[index] = DefaultRow[index];
        }
      }

      AddRow(newRow);
    }
    public void AddCartesian(GridLine gridLine)
    {
      if (gridLine.baseLine is not Line line)
      {
        throw new ArgumentException("Non line based gridlines are not supported");
      }

      var ux = line.start.x - line.end.x;
      var uy = line.start.y - line.end.y;

      // get rotation from global x and y in the counter-clockwise direction
      double gridRotation;
      if (Math.Abs(ux) > .01)
      {
        gridRotation = Math.Atan(uy / ux);
      }
      else
      {
        gridRotation = Math.PI / 2;
      }

      var gridSystem = GetExistingGridSystem(gridRotation) ?? CreateGridSystem(gridRotation);
      var transform = GetTransformFromGridSystem(gridSystem);
      _ = line.TransformTo(transform.Inverse(), out Line transformedLine);

      var newUx = Math.Abs(transformedLine.start.x - transformedLine.end.x);
      var newUy = Math.Abs(transformedLine.start.y - transformedLine.end.y);

      string lineType;
      double gridLineOffset;
      if (newUx < .1)
      {
        lineType = XGridLineType;
        gridLineOffset = toNativeScalingService
          .ScaleLength(transformedLine.start.x, transformedLine.units ?? transformedLine.start.units);
      }
      else if (newUy < .1)
      {
        lineType = YGridLineType;
        gridLineOffset = toNativeScalingService
          .ScaleLength(transformedLine.start.y, transformedLine.units ?? transformedLine.start.units);
      }
      else
      {
        throw new SpeckleException($"Error in transforming line from global coordinates to grid system with rotation {gridSystem.Rotation} and x,y offsets {gridSystem.XOrigin}, {gridSystem.YOrigin}");
      }

      AddCartesian(gridSystem.Name, lineType, gridLine.label, gridLineOffset);
    }

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

        if (combinedRotationsRemainder < .1 || combinedRotationsRemainder > .9)
        {
          return new GridSystemRepresentation(gridSysName, GridType.None, xOrigin, yOrigin, rotationRad);
        }
      }
      // could not find compatible existing grid system
      return null;
    }

    private GridSystemRepresentation CreateGridSystem(double gridRotation)
    {
      var systemName = $"SpeckleGridSystem{numberOfGridSystems++}";
      _ = cSapModel.GridSys.SetGridSys(systemName, 0, 0, gridRotation * 180 / Math.PI);

      // when a grid system is created, it doesn't show up unless it has at least one grid in each direction
      AddCartesian(systemName, XGridLineType, "Default0", 0, "No");
      AddCartesian(systemName, YGridLineType, "Default1", 0, "No");
      return new GridSystemRepresentation(systemName, GridType.None, 0, 0, gridRotation);
    }

    private Transform GetTransformFromGridSystem(GridSystemRepresentation sys)
    {   
      return new Transform(
        new double[]
        {
          Math.Cos(sys.Rotation), -Math.Sin(sys.Rotation), 0, sys.XOrigin,
          Math.Sin(sys.Rotation), Math.Cos(sys.Rotation), 0, sys.YOrigin,
          0, 0, 1, 0,
          0, 0, 0, 1
        }
      );
    }
  }

  public class GridSystemRepresentation
  {
    public GridSystemRepresentation(string name, GridType type, double xOrigin, double yOrigin, double rotation)
    {
      Name = name;
      Type = type;
      XOrigin = xOrigin;
      YOrigin = yOrigin;
      Rotation = rotation;
    }

    public string Name { get; }
    public GridType Type { get; }
    public double XOrigin { get; set; }
    public double YOrigin { get; set; }
    public double Rotation { get; set; }
  }

  public enum GridType
  {
    None = 0,
    Cartesian = 1,
    Cylindrical = 2
  }
}
