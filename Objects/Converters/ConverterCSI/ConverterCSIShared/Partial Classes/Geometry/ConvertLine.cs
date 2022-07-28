using System;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Analysis;
using CSiAPIv1;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    public object LineToNative(Line line)
    {

      string newFrame = "";
      Point end1node = line.start;
      Point end2node = line.end;
      Model.FrameObj.AddByCoord(
        ScaleToNative(end1node.x, end1node.units),
        ScaleToNative(end1node.y, end1node.units),
        ScaleToNative(end1node.z, end1node.units),
        ScaleToNative(end2node.x, end2node.units),
        ScaleToNative(end2node.y, end2node.units),
        ScaleToNative(end2node.z, end2node.units),
        ref newFrame
      );
      return line.applicationId;
    }

  }
}