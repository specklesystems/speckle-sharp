using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Objects.Geometry;
using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Helpers;
using Speckle.Core.Models;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public ApplicationObject UpdateParameter(ParameterUpdater paramUpdater)
    {
      //the below does not work because ApplicationIds are stored within a stream
      //try to get element using ApplicationId
      //this will only work if the element has been successfully been received in Revit before
      //var element = GetExistingElementByApplicationId(paramUpdater.revitId);

      var appObj = new ApplicationObject(paramUpdater.id, paramUpdater.speckle_type)
      {
        applicationId = paramUpdater.applicationId
      };
      Element element = null;

      //try to get element using ElementId
      if (int.TryParse(paramUpdater.elementId, out int intId))
      {
        var elemId = new ElementId(intId);
        element = Doc.GetElement(elemId);
      }

      //try to get element using UniqueId
      element ??= Doc.GetElement(paramUpdater.elementId);

      if (element != null)
      {
        SetInstanceParameters(element, paramUpdater);
        appObj.Update(
          status: ApplicationObject.State.Updated,
          logItem: $"Successfully updated instance parameters for element {paramUpdater.elementId}"
        );
      }
      else
      {
        appObj.Update(
          status: ApplicationObject.State.Failed,
          logItem: $"Could not find element to update: Element Id = {paramUpdater.elementId}"
        );
      }

      return appObj;
    }
  }
}
