using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using ConverterRevitShared.Revit;
using Objects.Geometry;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;
using DirectShape = Objects.BuiltElements.Revit.DirectShape;
using Mesh = Objects.Geometry.Mesh;
using Parameter = Objects.BuiltElements.Revit.Parameter;

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

      var converted = new List<DB.GeometryObject>();

      speckleDs.baseGeometries.ToList().ForEach(b =>
      {
        switch (b)
        {
          case Brep brep:
            try
            {
              var solid = BrepToNative(brep);
              converted.Add(solid);
            }
            catch (Exception e)
            {
              var mesh = MeshToNative(brep.displayMesh);
              converted.AddRange(mesh);
            }
            break;
          case Mesh mesh:
            var rMesh = MeshToNative(mesh);
            converted.AddRange(rMesh);
            break;
          default:
            Report.LogConversionError(new Exception($"Incompatible geometry type: {b.GetType()} is not supported in DirectShape conversions."));
            break;
        }
      });

      BuiltInCategory cat;
      var bic = Categories.GetBuiltInFromSchemaBuilderCategory(speckleDs.category);
      BuiltInCategory.TryParse(bic, out cat);
      var catId = Doc.Settings.Categories.get_Item(cat).Id;

      var revitDs = DB.DirectShape.CreateElement(Doc, catId);
      revitDs.ApplicationId = speckleDs.applicationId;
      revitDs.ApplicationDataId = Guid.NewGuid().ToString();
      revitDs.SetShape(converted);
      revitDs.Name = speckleDs.name;
      SetInstanceParameters(revitDs, speckleDs);
      //Report.Log($"Created DirectShape {revitDs.Id}");
      return new ApplicationPlaceholderObject { applicationId = speckleDs.applicationId, ApplicationGeneratedId = revitDs.UniqueId, NativeObject = revitDs };
    }

    // This is to support raw geometry being sent to Revit (eg from rhino, gh, autocad...)
    public ApplicationPlaceholderObject DirectShapeToNative(Brep brep, BuiltInCategory cat = BuiltInCategory.OST_GenericModel)
    {
      // if it comes from GH it doesn't have an applicationId, the use the hash id
      if (brep.applicationId == null)
        brep.applicationId = brep.id;

      var docObj = GetExistingElementByApplicationId(brep.applicationId);

      //just create new one 
      if (docObj != null)
      {
        Doc.Delete(docObj.Id);
      }

      var catId = Doc.Settings.Categories.get_Item(cat).Id;
      var revitDs = DB.DirectShape.CreateElement(Doc, catId);
      revitDs.ApplicationId = brep.applicationId;
      revitDs.ApplicationDataId = Guid.NewGuid().ToString();
      revitDs.Name = "Brep " + brep.applicationId;

      try
      {
        var solid = BrepToNative(brep);
        if (solid == null) throw new SpeckleException("Could not convert brep to native");
        revitDs.SetShape(new List<GeometryObject> { solid });
      }
      catch (Exception e)
      {
        Report.LogConversionError(new Exception(e.Message));
        var mesh = MeshToNative(brep.displayMesh);
        revitDs.SetShape(mesh);
      }
      //Report.Log($"Converted DirectShape {revitDs.Id}");
      return new ApplicationPlaceholderObject { applicationId = brep.applicationId, ApplicationGeneratedId = revitDs.UniqueId, NativeObject = revitDs };
    }

    // This is to support raw geometry being sent to Revit (eg from rhino, gh, autocad...)
    public ApplicationPlaceholderObject DirectShapeToNative(Mesh mesh, BuiltInCategory cat = BuiltInCategory.OST_GenericModel)
    {
      // if it comes from GH it doesn't have an applicationId, the use the hash id
      if (mesh.applicationId == null)
        mesh.applicationId = mesh.id;

      var docObj = GetExistingElementByApplicationId(mesh.applicationId);

      //just create new one 
      if (docObj != null)
      {
        Doc.Delete(docObj.Id);
      }

      var converted = new List<GeometryObject>();
      var rMesh = MeshToNative(mesh);
      converted.AddRange(rMesh);

      var catId = Doc.Settings.Categories.get_Item(cat).Id;

      var revitDs = DB.DirectShape.CreateElement(Doc, catId);
      revitDs.ApplicationId = mesh.applicationId;
      revitDs.ApplicationDataId = Guid.NewGuid().ToString();
      revitDs.SetShape(converted);
      revitDs.Name = "Mesh " + mesh.applicationId;
      Report.Log($"Converted DirectShape {revitDs.Id}");
      return new ApplicationPlaceholderObject { applicationId = mesh.applicationId, ApplicationGeneratedId = revitDs.UniqueId, NativeObject = revitDs };
    }

    private Mesh SolidToSpeckleMesh(Solid solid)
    {
      var mesh = new Mesh();
      (mesh.faces, mesh.vertices) = GetFaceVertexArrFromSolids(new List<Solid> { solid });
      mesh.units = ModelUnits;
      return mesh;
    }

    private DirectShape DirectShapeToSpeckle(DB.DirectShape revitAc)
    {
      var cat = ((BuiltInCategory)revitAc.Category.Id.IntegerValue).ToString();
      var category = Categories.GetSchemaBuilderCategoryFromBuiltIn(cat);
      var element = revitAc.get_Geometry(new Options());
      var geometries = element.ToList().Select<GeometryObject, Base>(obj =>
        {
          return obj
          switch
          {
            DB.Mesh mesh => MeshToSpeckle(mesh),
            Solid solid => SolidToSpeckleMesh(solid), // Should be replaced with 'BrepToSpeckle' when it works.
            _ => null
          };
        });
      var speckleAc = new DirectShape(
        revitAc.Name,
        category,
        geometries.ToList()
      );
      GetAllRevitParamsAndIds(speckleAc, revitAc);
      speckleAc["type"] = revitAc.Name;
      return speckleAc;
    }
  }
}
