using System;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.CSI.Geometry;
using Objects.BuiltElements;
using ConverterCSIShared.Models;
using Objects.Other;

namespace Objects.Converter.CSI;

public partial class ConverterCSI
{
  public void GridLineToNative(GridLine gridline)
  {
    GridLineDefinitionTable.AddCartesian(gridline);
  }

  public CSIGridLines gridLinesToSpeckle(string name)
  {
    double Xo = 0;
    double Yo = 0;
    double RZ = 0;
    string GridSysType = null;
    int NumXLines = 0;
    int NumYLines = 0;
    string[] GridLineIDX = null;
    string[] GridLineIDY = null;
    double[] OrdinateX = null;
    double[] OrdinateY = null;
    bool[] VisibleX = null;
    bool[] VisibleY = null;
    string[] BubbleLocX = null;
    string[] BubbleLocY = null;

    Model.GridSys.GetGridSys_2(
      name,
      ref Xo,
      ref Yo,
      ref RZ,
      ref GridSysType,
      ref NumXLines,
      ref NumYLines,
      ref GridLineIDX,
      ref GridLineIDY,
      ref OrdinateX,
      ref OrdinateY,
      ref VisibleX,
      ref VisibleY,
      ref BubbleLocX,
      ref BubbleLocY
    );

    var gridlines = new List<GridLine> { };
    CSIGridLines speckleGridLines = new();
    speckleGridLines.gridLines = gridlines;

    if (GridSysType == "Cartesian")
    {
      // Create transformation matrix
      var gridSystem = new GridSystemRepresentation(name, Xo, Yo, RZ * Math.PI / 180);
      var transform = GetTransformFromGridSystem(gridSystem);

      for (int i = 0; i < NumXLines; i++)
      {
        var line = new Line(
          new Point(OrdinateX[i], OrdinateY[0], units: ModelUnits()),
          new Point(OrdinateX[i], OrdinateY[NumYLines - 1], units: ModelUnits()),
          ModelUnits()
        );
        if (line.TransformTo(transform, out Line transformedLine)) // Maybe it's an orthogonal system and doesn't need to be transformed
        {
          var gridLine = new GridLine(transformedLine) { label = GridLineIDX[i] };
          speckleGridLines.gridLines.Add(gridLine);
        }
      }
      for (int j = 0; j < NumYLines; j++)
      {
        var line = new Line(
          new Point(OrdinateX[0], OrdinateY[j], units: ModelUnits()),
          new Point(OrdinateX[NumXLines - 1], OrdinateY[j], units: ModelUnits()),
          ModelUnits()
        );
        if (line.TransformTo(transform, out Line transformedLine)) // Maybe it's an orthogonal system and doesn't need to be transformed
        {
          var gridLine = new GridLine(transformedLine) { label = GridLineIDY[j] };
          speckleGridLines.gridLines.Add(gridLine);
        }
      }
    }
    SpeckleModel.elements.Add(speckleGridLines);
    return speckleGridLines;
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
