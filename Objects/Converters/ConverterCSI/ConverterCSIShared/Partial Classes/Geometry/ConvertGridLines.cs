using System;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Analysis;
using Speckle.Core.Models;
using Objects.Structural.CSI.Geometry;
using Objects.Structural.CSI.Properties;
using Objects.BuiltElements;
using System.Linq;
using CSiAPIv1;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    public void gridLinesToNative(CSIGridLines gridlines)
    {
      throw new NotSupportedException();
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

      Model.GridSys.GetGridSys_2(name, ref Xo, ref Yo, ref RZ, ref GridSysType, ref NumXLines, ref NumYLines, ref GridLineIDX, ref GridLineIDY, ref OrdinateX, ref OrdinateY, ref VisibleX, ref VisibleY, ref BubbleLocX, ref BubbleLocY);

      var gridlines = new List<GridLine> { };
      CSIGridLines speckleGridLines = new CSIGridLines();
      speckleGridLines.gridLines = gridlines;

      if (GridSysType == "Cartesian")
      {
        for (int i = 0; i < NumXLines; i++)
        {
          var pt1 = new Point(OrdinateX[i], OrdinateY[0], units: ModelUnits());
          var pt2 = new Point(OrdinateX[i], OrdinateY[NumYLines - 1], units: ModelUnits());
          var gridline = new GridLine(new Line(pt1, pt2, ModelUnits()));
          gridline.label = GridLineIDX[i];
          speckleGridLines.gridLines.Add(gridline);
        }
        for (int j = 0; j < NumYLines; j++)
        {
          var pt1 = new Point(OrdinateX[0], OrdinateY[j], units: ModelUnits());
          var pt2 = new Point(OrdinateX[NumXLines - 1], OrdinateY[j], units: ModelUnits());
          var gridline = new GridLine(new Line(pt1, pt2, ModelUnits()));
          gridline.label = GridLineIDY[j];
          speckleGridLines.gridLines.Add(gridline);
        }
        speckleGridLines.GridSystemType = GridSysType;
        speckleGridLines.Xo = Xo;
        speckleGridLines.Yo = Yo;
        speckleGridLines.Rz = RZ;
      }


      SpeckleModel.elements.Add(speckleGridLines);
      return speckleGridLines;
    }
  }
}