using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using Element = Objects.BuiltElements.Element;
using Objects.Revit;
using System.Linq;
using Objects.Geometry;
using System;
using Objects;
using DirectShape = Objects.Revit.DirectShape;
using System.Collections.Generic;
using Mesh = Objects.Geometry.Mesh;
using Speckle.Core.Models;

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
      IList<GeometryObject> mesh = MeshToNative(speckleDs.baseGeometry as Mesh);

      var cat = BuiltInCategory.OST_GenericModel;
      var bic = RevitUtils.GetBuiltInCategory(speckleDs.category);

      BuiltInCategory.TryParse(bic, out cat);
      var catId = Doc.Settings.Categories.get_Item(cat).Id;

      var revitDs = DB.DirectShape.CreateElement(Doc, catId);
      revitDs.ApplicationId = speckleDs.applicationId;
      revitDs.ApplicationDataId = Guid.NewGuid().ToString();
      revitDs.SetShape(mesh);
      revitDs.Name = speckleDs.type;

      SetElementParams(revitDs, speckleDs);
 
      return new ApplicationPlaceholderObject { applicationId = speckleDs.applicationId, ApplicationGeneratedId = revitDs.UniqueId };
    }

    private IRevit DirectShapeToSpeckle(DB.DirectShape revitAc)
    {
      var speckleAc = new DirectShape();
      speckleAc.type = revitAc.Name;
      var cat = ((BuiltInCategory)revitAc.Category.Id.IntegerValue).ToString();
      speckleAc.category = RevitUtils.GetCategory(cat);
      speckleAc.displayMesh = GetElementMesh(revitAc);
      speckleAc.baseGeometry = speckleAc.displayMesh;

      AddCommonRevitProps(speckleAc, revitAc);

      return speckleAc;
    }

  }
}
