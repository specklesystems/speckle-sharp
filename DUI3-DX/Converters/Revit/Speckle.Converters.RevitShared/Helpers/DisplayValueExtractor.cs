using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Logging;
using Mesh = Objects.Geometry.Mesh;

namespace Speckle.Converters.RevitShared.Helpers;

public sealed class DisplayValueExtractor
{
  private readonly IRawConversion<List<DB.Solid>, List<Mesh>> _solidsConversion;
  private readonly IRawConversion<List<DB.Mesh>, List<Mesh>> _meshConversion;

  public DisplayValueExtractor(
    IRawConversion<List<Solid>, List<Mesh>> solidsConversion,
    IRawConversion<List<DB.Mesh>, List<Mesh>> meshConversion
  )
  {
    _solidsConversion = solidsConversion;
    _meshConversion = meshConversion;
  }

  public List<Mesh> GetDisplayValue(
    DB.Element element,
    DB.Options? options = null,
    bool isConvertedAsInstance = false,
    DB.Transform transform = null
  )
  {
    var displayMeshes = new List<Mesh>();

    // test if the element is a group first
    if (element is Group g)
    {
      foreach (var id in g.GetMemberIds())
      {
        var groupMeshes = GetElementDisplayValue(element.Document.GetElement(id), options, isConvertedAsInstance);
        displayMeshes.AddRange(groupMeshes);
      }
      return displayMeshes;
    }

    var (solids, meshes) = GetSolidsAndMeshesFromElement(element, options, transform);

    // convert meshes and solids
    displayMeshes.AddRange(_meshConversion.RawConvert(meshes));
    displayMeshes.AddRange(_solidsConversion.RawConvert(solids));

    return displayMeshes;
  }

  /// <summary>
  /// Retreives the meshes on an element to use as the speckle displayvalue
  /// </summary>
  /// <param name="element"></param>
  /// <param name="isConvertedAsInstance">Some FamilyInstance elements are treated as proper Instance objects, while others are not. For those being converted as Instance objects, retrieve their display value untransformed by the instance transform or by the selected document reference point.</param>
  /// <returns></returns>
  /// <remarks>
  /// See https://www.revitapidocs.com/2023/e0f15010-0e19-6216-e2f0-ab7978145daa.htm for a full Geometry Object inheritance
  /// </remarks>
  private List<Mesh> GetElementDisplayValue(
    DB.Element element,
    Options options = null,
    bool isConvertedAsInstance = false,
    DB.Transform transform = null
  )
  {
    var displayMeshes = new List<Mesh>();

    // test if the element is a group first
    if (element is Group g)
    {
      foreach (var id in g.GetMemberIds())
      {
        var groupMeshes = GetElementDisplayValue(element.Document.GetElement(id), options, isConvertedAsInstance);
        displayMeshes.AddRange(groupMeshes);
      }
      return displayMeshes;
    }

    var (solids, meshes) = GetSolidsAndMeshesFromElement(element, options, transform);

    // convert meshes and solids
    displayMeshes.AddRange(_meshConversion.RawConvert(meshes));
    displayMeshes.AddRange(_solidsConversion.RawConvert(solids));

    return displayMeshes;
  }

  private (List<Solid>, List<DB.Mesh>) GetSolidsAndMeshesFromElement(
    Element element,
    Options options,
    Transform? transform = null
  )
  {
    //options = ViewSpecificOptions ?? options ?? new Options() { DetailLevel = DetailLevelSetting };
    options ??= new Options() { DetailLevel = ViewDetailLevel.Fine };

    GeometryElement geom;
    try
    {
      geom = element.get_Geometry(options);
    }
    catch (Autodesk.Revit.Exceptions.ArgumentException)
    {
      options.ComputeReferences = false;
      geom = element.get_Geometry(options);
    }

    var solids = new List<Solid>();
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
  private void SortGeometry(
    Element element,
    List<Solid> solids,
    List<DB.Mesh> meshes,
    GeometryElement geom,
    Transform inverseTransform = null
  )
  {
    var topLevelSolidsCount = 0;
    var topLevelMeshesCount = 0;
    var topLevelGeomElementCount = 0;
    var topLevelGeomInstanceCount = 0;
    bool hasSymbolGeometry = false;

    foreach (GeometryObject geomObj in geom)
    {
      switch (geomObj)
      {
        case Solid solid:
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
            solid = SolidUtils.CreateTransformed(solid, inverseTransform);
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
        case GeometryInstance instance:
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
        case GeometryElement geometryElement:
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
        topLevelGeomInstanceCount,
        hasSymbolGeometry
      );
    }
  }

  private static void LogInstanceMeshRetrievalWarnings(
    Element element,
    int topLevelSolidsCount,
    int topLevelMeshesCount,
    int topLevelGeomElementCount,
    int topLevelGeomInstanceCount,
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
  private Dictionary<string, GraphicsStyle> _graphicStyleCache = new();

  /// <summary>
  /// Exclude light source cones and potentially other geometries by their graphic style
  /// </summary>
  /// <param name="id"></param>
  /// <param name="doc"></param>
  /// <returns></returns>
  private bool IsSkippableGraphicStyle(ElementId id, Document doc)
  {
    if (!_graphicStyleCache.ContainsKey(id.ToString()))
    {
      _graphicStyleCache.Add(id.ToString(), doc.GetElement(id) as GraphicsStyle);
    }

    var graphicStyle = _graphicStyleCache[id.ToString()];

    if (
      graphicStyle != null
      && graphicStyle.GraphicsStyleCategory.Id.IntegerValue == (int)(BuiltInCategory.OST_LightingFixtureSource)
    )
    {
      return true;
    }

    return false;
  }
}
