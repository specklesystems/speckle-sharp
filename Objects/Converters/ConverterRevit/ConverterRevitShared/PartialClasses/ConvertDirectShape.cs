using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Objects.Geometry;
using Objects.Other;
using RevitSharedResources.Extensions.SpeckleExtensions;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;
using DirectShape = Objects.BuiltElements.Revit.DirectShape;
using Mesh = Objects.Geometry.Mesh;

namespace Objects.Converter.Revit;

public partial class ConverterRevit
{
  /// <summary>
  /// Attempts to convert a DirectShape to Revit according to the current ToNativeMeshSetting value.
  /// </summary>
  /// <param name="o">The direct shape to convert</param>
  /// <returns>An application placeholder object containing a DirectShape, DXF Import or Family containing DXF Import.</returns>
  public ApplicationObject TryDirectShapeToNative(DirectShape o, ToNativeMeshSettingEnum fallbackSetting)
  {
    try
    {
      // Try to convert to direct shape, taking into account the current mesh settings
      return DirectShapeToNative(o, fallbackSetting);
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

  public ApplicationObject TryDirectShapeToNative(
    Brep brep,
    ToNativeMeshSettingEnum fallbackSetting,
    RevitCategory cat = RevitCategory.GenericModel
  )
  {
    DirectShape ds =
      new($"Brep {brep.applicationId ?? brep.id}", cat, new List<Base> { brep })
      {
        applicationId = brep.applicationId,
        id = brep.id
      };
    return TryDirectShapeToNative(ds, fallbackSetting);
  }

  public ApplicationObject TryDirectShapeToNative(
    Mesh mesh,
    ToNativeMeshSettingEnum fallbackSetting,
    RevitCategory cat = RevitCategory.GenericModel
  )
  {
    DirectShape ds =
      new($"Mesh {mesh.applicationId ?? mesh.id}", cat, new List<Base> { mesh })
      {
        applicationId = mesh.applicationId,
        id = mesh.id
      };
    return TryDirectShapeToNative(ds, fallbackSetting);
  }

  public ApplicationObject TryDirectShapeToNative(
    ApplicationObject appObj,
    List<Mesh> meshes,
    ToNativeMeshSettingEnum fallbackSetting,
    RevitCategory cat = RevitCategory.GenericModel
  )
  {
    if (meshes.Count == 0)
    {
      appObj.Update(status: ApplicationObject.State.Failed, logItem: "Contained no meshes");
      return appObj;
    }

    var ds = new DirectShape(
      $"{appObj.Descriptor.Split(':').LastOrDefault() ?? "Meshes"} {appObj.applicationId}",
      cat,
      meshes.Cast<Base>().ToList()
    )
    {
      applicationId = appObj.applicationId,
      id = appObj.OriginalId
    };

    return TryDirectShapeToNative(ds, fallbackSetting);
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
    // get any existing elements. This could be a DirectShape, OR another element if using fallback receive
    var existingObj = GetExistingElementByApplicationId(speckleDs.applicationId ??= speckleDs.id);
    var appObj = new ApplicationObject(speckleDs.id, speckleDs.speckle_type)
    {
      applicationId = speckleDs.applicationId
    };

    // skip if element already exists in doc & receive mode is set to ignore
    if (IsIgnore(existingObj, appObj))
    {
      return appObj;
    }

    var converted = new List<GeometryObject>();

    speckleDs.baseGeometries
      .ToList()
      .ForEach(b =>
      {
        switch (b)
        {
          case Brep brep:
            try
            {
              var solid = BrepToNative(brep, out var notes);
              converted.Add(solid);
            }
            catch (Exception e)
            {
              if (fallback != ToNativeMeshSettingEnum.Default)
              {
                throw new FallbackToDxfException(
                  "Failed to convert BREP to Solid. Falling back to DXF import as per settings.",
                  e
                );
              }

              var mesh = brep.displayValue.SelectMany(
                m => MeshToNative(m, parentMaterial: brep["renderMaterial"] as RenderMaterial)
              );
              converted.AddRange(mesh);
            }

            break;
          case Mesh mesh:
            if (fallback != ToNativeMeshSettingEnum.Default)
            {
              throw new FallbackToDxfException(
                "DirectShape contains Mesh. Falling back to DXF import as per Settings."
              );
            }

            var rMesh = MeshToNative(mesh);
            converted.AddRange(rMesh);
            break;
          case ICurve curve:
            var rCurves = CurveToNative(curve, true);
            for (var i = 0; i < rCurves.Size; i++)
            {
              converted.Add(rCurves.get_Item(i));
            }

            break;
          default:
            appObj.Update(
              logItem: $"Incompatible geometry type: {b.GetType()} is not supported in DirectShape conversions."
            );
            break;
        }
      });

    if (existingObj != null && existingObj is DB.DirectShape existingDS) // if it's a directShape, just update
    {
      existingDS.SetShape(converted);
      SetInstanceParameters(existingDS, speckleDs);
      appObj.Update(status: ApplicationObject.State.Updated, createdId: existingDS.UniqueId, convertedItem: existingDS);
      return appObj;
    }

    //from 2.16 onwards use the builtInCategory field for direct shape fallback
    BuiltInCategory bic = BuiltInCategory.OST_GenericModel;
    if (!BuiltInCategory.TryParse(speckleDs["builtInCategory"] as string, out bic))
    {
      //pre 2.16 or coming from grasshopper, using the enum
      //TODO: move away from enum logic
      if ((int)speckleDs.category != -1)
      {
        var bicName = Categories.GetBuiltInFromSchemaBuilderCategory(speckleDs.category);
        _ = BuiltInCategory.TryParse(bicName, out bic);
      }
    }

    var cat = Doc.Settings.Categories.get_Item(bic);

    try
    {
      var revitDs = DB.DirectShape.CreateElement(Doc, cat.Id);
      if (speckleDs.applicationId != null)
      {
        revitDs.ApplicationId = speckleDs.applicationId;
      }

      revitDs.ApplicationDataId = Guid.NewGuid().ToString();
      revitDs.SetShape(converted);
      revitDs.Name = speckleDs.name;
      SetInstanceParameters(revitDs, speckleDs);
      // delete any existing objs
      if (existingObj != null)
      {
        try
        {
          Doc.Delete(existingObj.Id);
        }
        catch (Autodesk.Revit.Exceptions.ArgumentException e)
        {
          appObj.Log.Add($"Could not delete existing object: {e.Message}");
        }
      }
      appObj.Update(status: ApplicationObject.State.Created, createdId: revitDs.UniqueId, convertedItem: revitDs);
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      SpeckleLog.Logger.LogDefaultError(ex);
      appObj.Update(status: ApplicationObject.State.Failed, logItem: $"{ex.Message}");
    }

    return appObj;
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

    var speckleAc = new DirectShape(revitAc.Name, category, geometries);

    //Find display values in geometries
    List<Base> displayValue = new();
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
