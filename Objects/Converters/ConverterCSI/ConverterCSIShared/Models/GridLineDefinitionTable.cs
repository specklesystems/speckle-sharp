using System;
using System.Linq;
using CSiAPIv1;
using Objects.BuiltElements;
using Objects.Geometry;
using Objects.Other;
using Speckle.Core.Logging;

namespace ConverterCSIShared.Models
{
  internal class ETABSGridLineDefinitionTable : DatabaseTableWrapper
  {
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
    public ETABSGridLineDefinitionTable(cSapModel cSapModel) : base(cSapModel) { }

    public const string XGridLineType = "X (Cartesian)";
    public const string YGridLineType = "Y (Cartesian)";
    public const string RGridLineType = "R (Cylindrical)";
    public const string TGridLineType = "T (Cylindrical)";
    public void AddCartesian(string gridSystemName, string gridLineType, string gridName, double location)
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

      var ux = Math.Abs(line.start.x - line.end.x);
      var uy = Math.Abs(line.start.y - line.end.y);

      // get rotation from global x and y
      double gridRotation;
      if (ux > .01)
      {
        gridRotation = Math.Asin(uy / ux);
      }
      else
      {
        gridRotation = 90;
      }

      var gridSystem = GetOrCreateGridSystem(gridRotation);
      var transform = GetTransformFromGridSystem(gridSystem);
      _ = line.TransformTo(transform, out Line transformedLine);

      var newUx = Math.Abs(transformedLine.start.x - transformedLine.end.x);
      var newUy = Math.Abs(transformedLine.start.y - transformedLine.end.y);

      string lineType;
      double location;
      if (newUx < .1)
      {
        lineType = XGridLineType;
        location = newUy;
      }
      else if (newUy < .1)
      {
        lineType = YGridLineType;
        location = newUx;
      }
      else
      {
        throw new SpeckleException($"Error in transforming line from global coordinates to grid system with rotation {gridSystem.Rotation} and x,y offsets {gridSystem.XOrigin}, {gridSystem.YOrigin}");
      }

      AddCartesian(gridSystem.Name, lineType, gridLine.label, location);
    }

    private GridSystemRepresentation GetOrCreateGridSystem(double gridRotation)
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

        var combinedRotationsNormalized = Math.Abs((rotationDeg - gridRotation) / 90);
        var combinedRotationsRemainder = combinedRotationsNormalized - Math.Floor(combinedRotationsNormalized);

        if (combinedRotationsRemainder < .1)
        {
          return new GridSystemRepresentation(gridSysName, GridType.None, xOrigin, yOrigin, rotationDeg);
        }
      }

      throw new SpeckleException("TODO : create grid system if missing");
    }

    private Transform GetTransformFromGridSystem(GridSystemRepresentation sys)
    {
      var rotationComponent = new Transform(new double[]
      {
        Math.Cos(sys.Rotation), -Math.Sin(sys.Rotation), 0, 0,
        Math.Sin(sys.Rotation), Math.Cos(sys.Rotation), 0, 0,
        0, 0, 1, 0,
        0, 0, 0, 1
      });

      var translationComponent = new Transform(new double[]
      {
        1, 0, 0, sys.XOrigin,
        0, 1, 0, sys.YOrigin,
        0, 0, 1, 0,
        0, 0, 0, 1
      });

      return translationComponent * rotationComponent;
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
