using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Objects.Geometry;
using System.Collections.Generic;
using System.Linq;


namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public void UpdateParameter(ParameterUpdater paramUpdater)
    {
      //the below does not work because ApplicationIds are stored within a stream
      //try to get element using ApplicationId
      //this will only work if the element has been successfully been received in Revit before
      //var element = GetExistingElementByApplicationId(paramUpdater.revitId);

      Element element = null;

      //try to get element using ElementId
      int intId;

      if (int.TryParse(paramUpdater.elementId, out intId))
      {
        var elemId = new ElementId(intId);
        element = Doc.GetElement(elemId);
      }

      //try to get element using UniqueId
      if (element == null)
      {
        element = Doc.GetElement(paramUpdater.elementId);
      }

      if (element == null)
      {
        ConversionErrors.Add(new System.Exception($"Could not find element to update: Element Id = {paramUpdater.elementId}"));
        return;
      }

      SetInstanceParameters(element, paramUpdater);
    }

  }
}