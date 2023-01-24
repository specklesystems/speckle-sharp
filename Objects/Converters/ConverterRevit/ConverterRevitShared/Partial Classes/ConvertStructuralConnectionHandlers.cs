using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using System.Collections.Generic;
using DB = Autodesk.Revit.DB;
using Structure = Autodesk.Revit.DB.Structure;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    private ApplicationObject StructuralConnectionHandlerToNative(StructuralConnectionHandler speckleConnectionHandler)
    {
      // I think the code below is mostly what we need to get this working. However, the 
      // StructuralConnectionHandler has a one to many relationship and we need to make sure
      // that every object that the connection is connected to has been converted before
      // this object is converted. Therefore I'm waiting on Jedd's new traversal function to make this
      // a reality for me before implementing the receiveToNative method.

      //var docObj = GetExistingElementByApplicationId(speckleConnectionHandler.applicationId);
      //var appObj = new ApplicationObject(speckleConnectionHandler.id, speckleConnectionHandler.speckle_type) { applicationId = speckleConnectionHandler.applicationId };

      //// skip if element already exists in doc & receive mode is set to ignore
      //if (IsIgnore(docObj, appObj, out appObj))
      //  return appObj;

      //if (!GetElementType(speckleConnectionHandler, appObj, out Structure.StructuralConnectionHandlerType connectionType))
      //{
      //  appObj.Update(status: ApplicationObject.State.Failed, logItem: "Unable to find a valid connection type in the Revit project");
      //  return appObj;
      //}

      //if (docObj != null)
      //{
      //  // TODO: Replace with actual update logic here
      //  Doc.Delete(docObj.Id);
      //}

      //var elIds = new List<DB.ElementId>();
      //foreach (var speckleEl in speckleConnectionHandler.connectedElements)
      //{
      //  var revitEl = GetExistingElementByApplicationId(speckleEl.applicationId);
      //  if (revitEl != null)
      //    elIds.Add(revitEl.Id);
      //}

      //Structure.StructuralConnectionHandler.Create(Doc, elIds, connectionType.Id);
      //appObj.Update(status: ApplicationObject.State.Created);
      //return appObj;

      return null;
    }
    private Base StructuralConnectionHandlerToSpeckle(DB.Structure.StructuralConnectionHandler revitConnection)
    {
      var type = revitConnection.Document.GetElement(revitConnection.GetTypeId()) as Structure.StructuralConnectionHandlerType;

      //var connectedElements = revitConnection.GetConnectedElementIds();

      var speckleConnection = new StructuralConnectionHandler() { 
        family = type.FamilyName,
        type = type.Name,
        //connectedElements = connectedElements
      };

      // Structural Connection Handlers are (supposedly) view specific which requires getting mesh by view
      // TODO: not guarenteed that the active view is what we need (3D view with fine detail where the element is visible
      speckleConnection.displayValue = GetElementDisplayMesh(revitConnection, new DB.Options() { ComputeReferences = false, View = Doc.ActiveView });

      GetAllRevitParamsAndIds(speckleConnection, revitConnection);
      
      return speckleConnection;
    }
  }
}
