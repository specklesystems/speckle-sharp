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

      if (ElementExistsWithApplicationId(@base.applicationId, out string name))
      {
        UpdateFrameLocation(name, baseLine.start, baseLine.end, appObj);
        SetProfileSection(name, @base, appObj, true);
      }
      else
      {
        CreateFrame(baseLine.start, baseLine.end, out var frameName, out var _, ref appObj);
        SetProfileSection(frameName, @base, appObj);
      }
    }

    public void SetProfileSection(string frameName, Base @base, ApplicationObject appObj, bool isUpdate = false)
    {
      string endMessage;
      if (isUpdate)
        endMessage = "Section was not updated.";
      else
        endMessage = "Default section was assigned.";

      var speckleObjectType = @base["type"] as string;

      if (string.IsNullOrWhiteSpace(speckleObjectType))
      {
        appObj.Update(logItem: $"Object has no type property. {endMessage}");
      }
      else if (!Property1DExists(speckleObjectType))
      {
        appObj.Update(logItem: $"Element type, {speckleObjectType}, is not present in ETABS model. {endMessage}");
      }
      else
      {
        Model.FrameObj.SetSection(frameName, speckleObjectType);
      }
    }
  }
}
