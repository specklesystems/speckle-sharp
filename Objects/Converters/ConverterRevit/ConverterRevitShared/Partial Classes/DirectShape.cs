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

      //TODO: support other geometries
      IList<GeometryObject> mesh = MeshToNative(speckleDs.baseGeometry);

      var cat = BuiltInCategory.OST_GenericModel;
      var bic = RevitUtils.GetBuiltInCategory(speckleDs.category);

      BuiltInCategory.TryParse(bic, out cat);
      var catId = Doc.Settings.Categories.get_Item(cat).Id;

      var revitDs = DB.DirectShape.CreateElement(Doc, catId);
      revitDs.ApplicationId = speckleDs.applicationId;
      revitDs.ApplicationDataId = Guid.NewGuid().ToString();
      revitDs.SetShape(mesh);
      revitDs.Name = speckleDs.name;

      SetInstanceParameters(revitDs, speckleDs);

      return new ApplicationPlaceholderObject { applicationId = speckleDs.applicationId, ApplicationGeneratedId = revitDs.UniqueId, NativeObject = revitDs };
    }

    private DirectShape DirectShapeToSpeckle(DB.DirectShape revitAc)
    {
      var speckleAc = new DirectShape();
      speckleAc.name = revitAc.Name;
      var cat = ((BuiltInCategory)revitAc.Category.Id.IntegerValue).ToString();
      speckleAc.category = RevitUtils.GetCategory(cat);
      speckleAc["@displayMesh"] = GetElementMesh(revitAc);
      speckleAc.baseGeometry = speckleAc["@displayMesh"] as Mesh;

      GetRevitParameters(speckleAc, revitAc);

      return speckleAc;
    }

  }
}
