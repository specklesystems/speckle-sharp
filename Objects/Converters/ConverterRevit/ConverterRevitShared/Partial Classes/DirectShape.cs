using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using DB = Autodesk.Revit.DB;
using DirectShape = Objects.BuiltElements.Revit.DirectShape;
using Mesh = Objects.Geometry.Mesh;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {

    public ApplicationPlaceholderObject DirectShapeToNative(DirectShape speckleDs)
    {
      var docObj = GetExistingElementByApplicationId(speckleDs.applicationId);

      //just create new one 
      if (docObj != null)
      {
        Doc.Delete(docObj.Id);
      }

      IList<GeometryObject> mesh;
      
      // Check if DS contains a solid or a mesh
      if (speckleDs.isSolid)
      {
        try
        {
          // Try to convert the BRep, this process can fail. If this happens we fallback to the mesh displayValue.
          // TODO: Once Brep conversion is sturdy enough, we should remove the displayValue logic.
          var brep = BrepToNative(speckleDs.solidGeometry);
          mesh = new List<GeometryObject> {brep};
        }
        catch (Exception e)
        {
          ConversionErrors.Add(new Error("Brep conversion failed: " + e.Message,e.InnerException?.Message ?? "No details available." ));
          mesh = MeshToNative(speckleDs.solidGeometry.displayValue);
        }
      }
      else
      {
        mesh = MeshToNative(speckleDs.baseGeometry);
      }
      
      var cat = BuiltInCategory.OST_GenericModel;
      var bic = RevitUtils.GetBuiltInCategory(speckleDs.category);

      BuiltInCategory.TryParse(bic, out cat);
      var catId = Doc.Settings.Categories.get_Item(cat).Id;

      var revitDs = DB.DirectShape.CreateElement(Doc, catId);
      revitDs.ApplicationId = speckleDs.applicationId;
      revitDs.ApplicationDataId = Guid.NewGuid().ToString();
      revitDs.SetShape(mesh);
      revitDs.Name = speckleDs.type;

      SetInstanceParameters(revitDs, speckleDs);

      return new ApplicationPlaceholderObject { applicationId = speckleDs.applicationId, ApplicationGeneratedId = revitDs.UniqueId, NativeObject = revitDs };
    }

    private DirectShape DirectShapeToSpeckle(DB.DirectShape revitAc)
    {
      var speckleAc = new DirectShape();
      speckleAc.type = revitAc.Name;
      var cat = ((BuiltInCategory)revitAc.Category.Id.IntegerValue).ToString();
      speckleAc.category = RevitUtils.GetCategory(cat);
      speckleAc["@displayMesh"] = GetElementMesh(revitAc);
      speckleAc.baseGeometry = speckleAc["@displayMesh"] as Mesh;

      AddCommonRevitProps(speckleAc, revitAc);

      return speckleAc;
    }

  }
}
