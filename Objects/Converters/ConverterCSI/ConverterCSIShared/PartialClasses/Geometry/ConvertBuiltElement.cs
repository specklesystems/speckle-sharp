using System;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.Converter.CSI;

public partial class ConverterCSI
{
  public void CurveBasedElementToNative(Base @base, ICurve curve, ApplicationObject appObj)
  {
    if (curve is not Line baseLine)
    {
      throw new ArgumentException("Only line based frames are currently supported", nameof(curve));
    }

    if (ElementExistsWithApplicationId(@base.applicationId, out string name))
    {
      UpdateFrameLocation(name, baseLine.start, baseLine.end, appObj);
      SetProfileSection(name, @base, appObj, true);
    }
    else
    {
      CreateFrame(baseLine.start, baseLine.end, out var frameName, out _, appObj);
      SetProfileSection(frameName, @base, appObj);
    }
  }

  public void SetProfileSection(string frameName, Base @base, ApplicationObject appObj, bool isUpdate = false)
  {
    string endMessage;
    if (isUpdate)
    {
      endMessage = "Section was not updated.";
    }
    else
    {
      endMessage = "Default section was assigned.";
    }

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
