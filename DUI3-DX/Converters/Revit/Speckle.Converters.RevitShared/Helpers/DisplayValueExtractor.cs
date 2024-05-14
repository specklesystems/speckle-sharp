using Speckle.Converters.Common.Objects;
using Speckle.Core.Logging;

namespace Speckle.Converters.RevitShared.Helpers;

// POC: needs breaking down https://spockle.atlassian.net/browse/CNX-9354
public sealed class DisplayValueExtractor
{
  private readonly IRawConversion<Dictionary<DB.ElementId, List<DB.Mesh>>, List<SOG.Mesh>> _meshByMaterialConverter;

  public DisplayValueExtractor(
    IRawConversion<Dictionary<DB.ElementId, List<DB.Mesh>>, List<SOG.Mesh>> meshByMaterialConverter
  )
  {
    _meshByMaterialConverter = meshByMaterialConverter;
  }

  public List<SOG.Mesh> GetDisplayValue(
    DB.Element element,
    DB.Options? options = null,
    // POC: should this be part of the context?
    DB.Transform? transform = null
  )
  {
    var displayMeshes = new List<SOG.Mesh>();

    // test if the element is a group first
    if (element is DB.Group g)
    {
      foreach (var id in g.GetMemberIds())
      {
        var groupMeshes = GetDisplayValue(element.Document.GetElement(id), options);
        displayMeshes.AddRange(groupMeshes);
      }
      return displayMeshes;
    }

    var (solids, meshes) = GetSolidsAndMeshesFromElement(element, options, transform);

    var meshesByMaterial = GetMeshesByMaterial(meshes, solids);

    return _meshByMaterialConverter.RawConvert(meshesByMaterial);
  }

  private static Dictionary<DB.ElementId, List<DB.Mesh>> GetMeshesByMaterial(
    List<DB.Mesh> meshes,
    List<DB.Solid> solids
  )
  {
    var meshesByMaterial = new Dictionary<DB.ElementId, List<DB.Mesh>>();
    foreach (var mesh in meshes)
    {
      var materialId = mesh.MaterialElementId;
      if (!meshesByMaterial.ContainsKey(materialId))
      {
        meshesByMaterial[materialId] = new List<DB.Mesh>();
      }

      meshesByMaterial[materialId].Add(mesh);
    }

    foreach (var solid in solids)
    {
      foreach (DB.Face face in solid.Faces)
      {
        var materialId = face.MaterialElementId;
        if (!meshesByMaterial.ContainsKey(materialId))
        {
          meshesByMaterial[materialId] = new List<DB.Mesh>();
        }

        meshesByMaterial[materialId].Add(face.Triangulate());
      }
    }

    return meshesByMaterial;
  }

  private (List<DB.Solid>, List<DB.Mesh>) GetSolidsAndMeshesFromElement(
    DB.Element element,
    DB.Options? options,
    DB.Transform? transform = null
  )
  {
    //options = ViewSpecificOptions ?? options ?? new Options() { DetailLevel = DetailLevelSetting };
    options ??= new DB.Options { DetailLevel = DB.ViewDetailLevel.Fine };

    DB.GeometryElement geom;
    try
    {
      geom = element.get_Geometry(options);
    }
    // POC: should we be trying to continue?
    catch (Autodesk.Revit.Exceptions.ArgumentException)
    {
      options.ComputeReferences = false;
      geom = element.get_Geometry(options);
    }

    var solids = new List<DB.Solid>();
    var meshes = new List<DB.Mesh>();

    if (geom != null)
    {
      // retrieves all meshes and solids from a geometry element
      SortGeometry(element, solids, meshes, geom, transform?.Inverse);
    }

    return (solids, meshes);
  }

  /// <summary>
  /// According to the remarks on the GeometryInstance class in the RevitAPIDocs,
  /// https://www.revitapidocs.com/2024/fe25b14f-5866-ca0f-a660-c157484c3a56.htm,
  /// a family instance geometryElement should have a top-level geometry instance when the symbol
  /// does not have modified geometry (the docs say that modified geometry will not have a geom instance,
  /// however in my experience, all family instances have a top-level geom instance, but if the family instance
  /// is modified, then the geom instance won't contain any geometry.)
  ///
  /// This remark also leads me to think that a family instance will not have top-level solids and geom instances.
  /// We are logging cases where this is not true.
  /// </summary>
  /// <param name="element"></param>
  /// <param name="solids"></param>
  /// <param name="meshes"></param>
  /// <param name="geom"></param>
  /// <param name="inverseTransform"></param>
  private void SortGeometry(
    DB.Element element,
    List<DB.Solid> solids,
    List<DB.Mesh> meshes,
    DB.GeometryElement geom,
    DB.Transform? inverseTransform = null
  )
  {
    var topLevelSolidsCount = 0;
    var topLevelMeshesCount = 0;
    var topLevelGeomElementCount = 0;
    var topLevelGeomInstanceCount = 0;
    bool hasSymbolGeometry = false;

    foreach (DB.GeometryObject geomObj in geom)
    {
      // POC: switch could possibly become factory and IIndex<,> pattern and move conversions to
      // separate IComeConversionInterfaces
      switch (geomObj)
      {
        case DB.Solid solid:
          // skip invalid solid
          if (
            solid.Faces.Size == 0
            || Math.Abs(solid.SurfaceArea) == 0
            || IsSkippableGraphicStyle(solid.GraphicsStyleId, element.Document)
          )
          {
            continue;
          }

          if (inverseTransform != null)
          {
            topLevelSolidsCount++;
            solid = DB.SolidUtils.CreateTransformed(solid, inverseTransform);
          }

          solids.Add(solid);
          break;
        case DB.Mesh mesh:
          if (IsSkippableGraphicStyle(mesh.GraphicsStyleId, element.Document))
          {
            continue;
          }

          if (inverseTransform != null)
          {
            topLevelMeshesCount++;
            mesh = mesh.get_Transformed(inverseTransform);
          }

          meshes.Add(mesh);
          break;
        case DB.GeometryInstance instance:
          // element transforms should not be carried down into nested geometryInstances.
          // Nested geomInstances should have their geom retreived with GetInstanceGeom, not GetSymbolGeom
          if (inverseTransform != null)
          {
            topLevelGeomInstanceCount++;
            SortGeometry(element, solids, meshes, instance.GetSymbolGeometry());
            if (meshes.Count > 0 || solids.Count > 0)
            {
              hasSymbolGeometry = true;
            }
          }
          else
          {
            SortGeometry(element, solids, meshes, instance.GetInstanceGeometry());
          }
          break;
        case DB.GeometryElement geometryElement:
          if (inverseTransform != null)
          {
            topLevelGeomElementCount++;
          }
          SortGeometry(element, solids, meshes, geometryElement);
          break;
      }
    }

    if (inverseTransform != null)
    {
      LogInstanceMeshRetrievalWarnings(
        element,
        topLevelSolidsCount,
        topLevelMeshesCount,
        topLevelGeomElementCount,
        hasSymbolGeometry
      );
    }
  }

  // POC: should be hoovered up with the new reporting, logging, exception philosophy
  private static void LogInstanceMeshRetrievalWarnings(
    DB.Element element,
    int topLevelSolidsCount,
    int topLevelMeshesCount,
    int topLevelGeomElementCount,
    bool hasSymbolGeom
  )
  {
    if (hasSymbolGeom)
    {
      if (topLevelSolidsCount > 0)
      {
        SpeckleLog.Logger.Warning(
          "Element of type {elementType} with uniqueId {uniqueId} has valid symbol geometry and {numSolids} top level solids. See comment on method SortInstanceGeometry for link to RevitAPI docs that leads us to believe this shouldn't happen",
          element.GetType(),
          element.UniqueId,
          topLevelSolidsCount
        );
      }
      if (topLevelMeshesCount > 0)
      {
        SpeckleLog.Logger.Warning(
          "Element of type {elementType} with uniqueId {uniqueId} has valid symbol geometry and {numMeshes} top level meshes. See comment on method SortInstanceGeometry for link to RevitAPI docs that leads us to believe this shouldn't happen",
          element.GetType(),
          element.UniqueId,
          topLevelMeshesCount
        );
      }
      if (topLevelGeomElementCount > 0)
      {
        SpeckleLog.Logger.Warning(
          "Element of type {elementType} with uniqueId {uniqueId} has valid symbol geometry and {numGeomElements} top level geometry elements. See comment on method SortInstanceGeometry for link to RevitAPI docs that leads us to believe this shouldn't happen",
          element.GetType(),
          element.UniqueId,
          topLevelGeomElementCount
        );
      }
    }
  }

  /// <summary>
  /// We're caching a dictionary of graphic styles and their ids as it can be a costly operation doing Document.GetElement(solid.GraphicsStyleId) for every solid
  /// </summary>
  private readonly Dictionary<string, DB.GraphicsStyle> _graphicStyleCache = new();

  /// <summary>
  /// Exclude light source cones and potentially other geometries by their graphic style
  /// </summary>
  /// <param name="id"></param>
  /// <param name="doc"></param>
  /// <returns></returns>
  private bool IsSkippableGraphicStyle(DB.ElementId id, DB.Document doc)
  {
    if (!_graphicStyleCache.ContainsKey(id.ToString()))
    {
      _graphicStyleCache.Add(id.ToString(), (DB.GraphicsStyle)doc.GetElement(id));
    }

    var graphicStyle = _graphicStyleCache[id.ToString()];

    if (
      graphicStyle != null
      && graphicStyle.GraphicsStyleCategory.Id.IntegerValue == (int)DB.BuiltInCategory.OST_LightingFixtureSource
    )
    {
      return true;
    }

    return false;
  }
}
