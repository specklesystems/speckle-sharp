using System;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Analysis;
using CSiAPIv1;
using Speckle.Core.Models;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    public void LineToNative(Line line, ref ApplicationObject appObj)
    {

      string newFrame = "";
      Point end1node = line.start;
      Point end2node = line.end;
      var success = Model.FrameObj.AddByCoord(
        ScaleToNative(end1node.x, end1node.units),
        ScaleToNative(end1node.y, end1node.units),
        ScaleToNative(end1node.z, end1node.units),
        ScaleToNative(end2node.x, end2node.units),
        ScaleToNative(end2node.y, end2node.units),
        ScaleToNative(end2node.z, end2node.units),
        ref newFrame
      );

      if (success == 0)
        appObj.Update(status: ApplicationObject.State.Created, createdId: $"{newFrame}");
      else
        appObj.Update(status: ApplicationObject.State.Failed);
    }

  }
}