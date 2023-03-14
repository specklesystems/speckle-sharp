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
    public void CurveBasedElementToNative(Base @base, ICurve curve, ref ApplicationObject appObj)
    {
      if (!(curve is Line baseLine))
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "Only line based frames are currently supported");
        return;
      }

      var frameObjectType = "Default";
      var speckleObjectType = @base["type"] as string;

      if (string.IsNullOrWhiteSpace(speckleObjectType))
      {
        appObj.Update("Object has no type property. Default section profile was assigned");
      }
      else if (!Property1DExists(speckleObjectType))
      {
        appObj.Update(logItem: $"Element type, {speckleObjectType}, is not present in ETABS model. Default section profile was assigned");
      }
      else
      {
        frameObjectType = speckleObjectType;
      }

      CreateFrame(baseLine.start, baseLine.end, out var _, out var _, ref appObj, frameObjectType);   
    }
  }
}
