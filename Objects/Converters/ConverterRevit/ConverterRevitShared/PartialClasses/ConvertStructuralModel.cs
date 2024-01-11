using Objects.Structural.Geometry;
using Speckle.Core.Models;
using Objects.Structural.Analysis;
using Objects.Structural.CSI.Geometry;
using RevitSharedResources.Extensions.SpeckleExtensions;
using Speckle.Core.Logging;
using System;

namespace Objects.Converter.Revit;

public partial class ConverterRevit
{
  public ApplicationObject StructuralModelToNative(Model speckleStructModel)
  {
    var lengthUnits = speckleStructModel.specs.settings.modelUnits.length;
    var appObj = new ApplicationObject(speckleStructModel.id, speckleStructModel.speckle_type)
    {
      applicationId = speckleStructModel.applicationId
    };
    foreach (Node node in speckleStructModel.nodes)
    {
      var _node = AnalyticalNodeToNative(node);
      appObj.Update(createdIds: _node.CreatedIds, converted: _node.Converted);
    }
    foreach (var element in speckleStructModel.elements)
    {
      if (element is Element1D element1D)
      {
        element1D.units = lengthUnits;

        try
        {
          if (element is CSIElement1D csiElement1D)
          {
            var _csiStick = AnalyticalStickToNative(csiElement1D);
            appObj.Update(createdIds: _csiStick.CreatedIds, converted: _csiStick.Converted);
          }
          else
          {
            var _stick = AnalyticalStickToNative((Element1D)element);
            appObj.Update(createdIds: _stick.CreatedIds, converted: _stick.Converted);
          }
        }
        catch (Exception ex) when (!ex.IsFatal())
        {
          SpeckleLog.Logger.LogDefaultError(ex);
        }
      }
      else
      {
        try
        {
          if (element is CSIElement2D csiElement2D)
          {
            var _csiStick = AnalyticalSurfaceToNative(csiElement2D);
            appObj.Update(createdIds: _csiStick.CreatedIds, converted: _csiStick.Converted);
          }
          else
          {
            var _stick = AnalyticalSurfaceToNative((Element2D)element);
            appObj.Update(createdIds: _stick.CreatedIds, converted: _stick.Converted);
          }
        }
        catch (Exception ex) when (!ex.IsFatal())
        {
          SpeckleLog.Logger.LogDefaultError(ex);
        }
      }
    }

    return appObj;
  }
}
