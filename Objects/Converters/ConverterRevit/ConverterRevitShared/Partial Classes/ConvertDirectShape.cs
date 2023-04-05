using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Objects.Geometry;
using Objects.Other;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;
using DirectShape = Objects.BuiltElements.Revit.DirectShape;
using Mesh = Objects.Geometry.Mesh;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    /// <summary>
    /// Attempts to convert a DirectShape to Revit according to the current ToNativeMeshSetting value.
    /// </summary>
    /// <param name="o">The direct shape to convert</param>
    /// <returns>An application placeholder object containing a DirectShape, DXF Import or Family containing DXF Import.</returns>
    public ApplicationObject TryDirectShapeToNative(DirectShape o)
    {
      try
      {
        // Try to convert to direct shape, taking into account the current mesh settings
        return DirectShapeToNative(o, ToNativeMeshSetting);
      }
      catch (FallbackToDxfException e)
      {
        Report.Log(e.Message);
        // FallbackToDxf exception means we should attempt a DXF import instead.
        switch (ToNativeMeshSetting)
        {
          case ToNativeMeshSettingEnum.DxfImport:
            return DirectShapeToDxfImport(o); // DirectShape -> DXF
          case ToNativeMeshSettingEnum.DxfImportInFamily:
            return DirectShapeToDxfImportFamily(o); // DirectShape -> Family (DXF inside)
          case ToNativeMeshSettingEnum.Default:
          default:
            // For anything else, try again with the default fallback (ugly meshes).
            return DirectShapeToNative(o, ToNativeMeshSettingEnum.Default);
        }
      }
    }

    /// <summary>
    /// The default DirectShape conversion method. Will return a Revit DirectShape with the containing geometry.
    /// </summary>
    /// <param name="speckleDs"></param>
    /// <param name="fallback"></param>
    /// <returns></returns>
    /// <exception cref="FallbackToDxfException"></exception>
    public ApplicationObject DirectShapeToNative(DirectShape speckleDs, ToNativeMeshSettingEnum fallback)
    {
      var existingDS = GetExistingElementByApplicationId(speckleDs.applicationId ??= speckleDs.id) as DB.DirectShape;
      var appObj =
        new ApplicationObject(speckleDs.id, speckleDs.speckle_type) { applicationId = speckleDs.applicationId };

      // skip if element already exists in doc & receive mode is set to ignore
      if (IsIgnore(existingDS, appObj, out appObj))
        return appObj;

      var converted = new List<DB.GeometryObject>();

      speckleDs.baseGeometries.ToList().ForEach(b =>
      {
        var notes = new List<string>();
        switch (b)
        {
          case Brep brep:
            try
            {
              var solid = BrepToNative(brep, out notes);
              converted.Add(solid);
            }
            catch (Exception e)
            {
              if (fallback != ToNativeMeshSettingEnum.Default)
                throw new FallbackToDxfException(
                  "Failed to convert BREP to Solid. Falling back to DXF import as per settings.");
              var mesh = brep.displayValue.SelectMany(m =>
                MeshToNative(m, parentMaterial: brep["renderMaterial"] as RenderMaterial));
              converted.AddRange(mesh);
            }

            break;
          case Mesh mesh:
            if (fallback != ToNativeMeshSettingEnum.Default)
              throw new FallbackToDxfException(
                "DirectShape contains Mesh. Falling back to DXF import as per Settings.");
            var rMesh = MeshToNative(mesh);
            converted.AddRange(rMesh);
            break;
          default:
            appObj.Update(
              logItem: $"Incompatible geometry type: {b.GetType()} is not supported in DirectShape conversions.");
            break;
        }
      });

      if (existingDS != null)
      {
        // Try to update the existing Direct Shape
        existingDS.SetShape(converted);
        appObj.Update(status: ApplicationObject.State.Updated, createdId: existingDS.UniqueId,
          convertedItem: existingDS);
        return appObj;
      }

      BuiltInCategory bic;
      if ((int)speckleDs.category == -1)
        speckleDs.category = RevitCategory.GenericModel;
      var bicName = Categories.GetBuiltInFromSchemaBuilderCategory(speckleDs.category);

      BuiltInCategory.TryParse(bicName, out bic);
      var cat = Doc.Settings.Categories.get_Item(bic);

      var revitDs = DB.DirectShape.CreateElement(Doc, cat.Id);
      revitDs.ApplicationId = speckleDs.applicationId;
      revitDs.ApplicationDataId = Guid.NewGuid().ToString();
      revitDs.SetShape(converted);
      revitDs.Name = speckleDs.name;
      SetInstanceParameters(revitDs, speckleDs);
      appObj.Update(status: ApplicationObject.State.Created, createdId: revitDs.UniqueId, convertedItem: revitDs);
      return appObj;
    }


    // This is to support raw geometry being sent to Revit (eg from rhino, gh, autocad...)
    public ApplicationObject DirectShapeToNative(Brep brep, RevitCategory cat = RevitCategory.GenericModel)
    {
      var speckleDS = new DirectShape(
        $"Brep {brep.applicationId ?? brep.id}",
        cat,
        new List<Base> { brep }) { applicationId = brep.applicationId, id = brep.id };

      return DirectShapeToNative(speckleDS, ToNativeMeshSetting);
    }

    // This is to support raw geometry being sent to Revit (eg from rhino, gh, autocad...)
    public ApplicationObject DirectShapeToNative(Mesh mesh)
      => DirectShapeToNative(new ApplicationObject(mesh.id, mesh.speckle_type){ applicationId = mesh.applicationId}, new[] { mesh });

    public ApplicationObject DirectShapeToNative(ApplicationObject appObj, IList<Mesh> meshes, RevitCategory cat = RevitCategory.GenericModel)
    {
      if (meshes.Count == 0)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "Contained no meshes");
        return appObj;
      }

      var speckleDS = new DirectShape(
        $"Meshes {appObj.applicationId}",
        cat,
        meshes.Cast<Base>().ToList()) { applicationId = appObj.applicationId, id = appObj.OriginalId };

      return DirectShapeToNative(speckleDS, ToNativeMeshSetting);
    }

    private DirectShape DirectShapeToSpeckle(DB.DirectShape revitAc)
    {
      var cat = ((BuiltInCategory)revitAc.Category.Id.IntegerValue).ToString();
      var category = Categories.GetSchemaBuilderCategoryFromBuiltIn(cat);
      var element = revitAc.get_Geometry(new Options());

      var geometries = new List<Base>();
      foreach (var obj in element)
      {
        switch (obj)
        {
          case DB.Mesh mesh:
            geometries.Add(MeshToSpeckle(mesh, revitAc.Document));
            break;
          case Solid solid: // TODO Should be replaced with 'BrepToSpeckle' when it works.
            geometries.AddRange(ConvertSolidsByRenderMaterial(new[] { solid }, revitAc.Document));
            break;
        }
      }

      var speckleAc = new DirectShape(
        revitAc.Name,
        category,
        geometries
      );

      //Find display values in geometries
      List<Base> displayValue = new List<Base>();
      foreach (Base geo in geometries)
      {
        switch (geo["displayValue"])
        {
          case null:
            //geo has no display value, we assume it is itself a valid displayValue
            displayValue.Add(geo);
            break;

          case Base b:
            displayValue.Add(b);
            break;

          case IEnumerable<Base> e:
            displayValue.AddRange(e);
            break;
        }
      }

      speckleAc.displayValue = displayValue;
      GetAllRevitParamsAndIds(speckleAc, revitAc);
      speckleAc["type"] = revitAc.Name;
      return speckleAc;
    }
  }
}
