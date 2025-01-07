using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Objects.Geometry;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;
using DirectShape = Objects.BuiltElements.Revit.DirectShape;
using Mesh = Objects.Geometry.Mesh;
using Parameter = Objects.BuiltElements.Revit.Parameter;
using Point = Objects.Geometry.Point;

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

      var converted = new List<GeometryObject>();

      speckleDs
        .baseGeometries.ToList()
        .ForEach(b =>
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
                var mesh = MeshToNative(brep.displayValue);
                converted.AddRange(mesh);
              }
              break;
            case Mesh mesh:
              var rMesh = MeshToNative(mesh);
              converted.AddRange(rMesh);
              break;
            default:
              ConversionErrors.Add(
                new Error(
                  "Incompatible geometry type",
                  $"Type ${b.GetType()} is not supported in DirectShape conversions."
                )
              );
              break;
          }
        });

      BuiltInCategory cat;
      var bic = RevitUtils.GetBuiltInCategory(speckleDs.category);
      BuiltInCategory.TryParse(bic, out cat);
      var catId = Doc.Settings.Categories.get_Item(cat).Id;

      var revitDs = DB.DirectShape.CreateElement(Doc, catId);
      revitDs.ApplicationId = speckleDs.applicationId;
      revitDs.ApplicationDataId = Guid.NewGuid().ToString();
      revitDs.SetShape(converted);
      revitDs.Name = speckleDs.name;
      SetInstanceParameters(revitDs, speckleDs);

      return new ApplicationPlaceholderObject
      {
        applicationId = speckleDs.applicationId,
        ApplicationGeneratedId = revitDs.UniqueId,
        NativeObject = revitDs
      };
    }

    private DirectShape DirectShapeToSpeckle(DB.DirectShape revitAc)
    {
      var cat = ((BuiltInCategory)revitAc.Category.Id.IntegerValue).ToString();
      var category = RevitUtils.GetCategory(cat);
      var element = revitAc.get_Geometry(new Options());
      var geometries = element
        .ToList()
        .Select<GeometryObject, Base>(obj =>
        {
          return obj switch
          {
            DB.Mesh mesh => MeshToSpeckle(mesh),
            Solid solid => BrepToSpeckle(solid),
            _ => null
          };
        });
      var speckleAc = new DirectShape(revitAc.Name, category, geometries.ToList(), new List<Parameter>());
      GetRevitParameters(speckleAc, revitAc);
      return speckleAc;
    }
  }
}
