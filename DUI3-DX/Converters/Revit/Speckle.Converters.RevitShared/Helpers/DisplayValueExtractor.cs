using System.Diagnostics.CodeAnalysis;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Logging;
using Speckle.InterfaceGenerator;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.RevitShared.Helpers;

// POC: needs breaking down https://spockle.atlassian.net/browse/CNX-9354
[GenerateAutoInterface]
public sealed class DisplayValueExtractor : IDisplayValueExtractor
{
  private readonly ITypedConverter<
    Dictionary<IRevitElementId, List<IRevitMesh>>,
    List<SOG.Mesh>
  > _meshByMaterialConverter;
  private readonly IRevitOptionsFactory _revitOptionsFactory;
  private readonly IRevitSolidUtils _revitSolidUtils;

  public DisplayValueExtractor(
    ITypedConverter<Dictionary<IRevitElementId, List<IRevitMesh>>, List<SOG.Mesh>> meshByMaterialConverter,
    IRevitOptionsFactory revitOptionsFactory,
    IRevitSolidUtils revitSolidUtils
  )
  {
    _meshByMaterialConverter = meshByMaterialConverter;
    _revitOptionsFactory = revitOptionsFactory;
    _revitSolidUtils = revitSolidUtils;
  }

  public List<SOG.Mesh> GetDisplayValue(
    IRevitElement element,
    IRevitOptions? options = null,
    // POC: should this be part of the context?
    IRevitTransform? transform = null
  )
  {
    var displayMeshes = new List<SOG.Mesh>();

    // test if the element is a group first
    var g = element.ToGroup();
    if (g is not null)
    {
      foreach (var id in g.GetMemberIds())
      {
        var groupMeshes = GetDisplayValue(element.Document.GetElement(id).NotNull(), options);
        displayMeshes.AddRange(groupMeshes);
      }
      return displayMeshes;
    }

    var (solids, meshes) = GetSolidsAndMeshesFromElement(element, options, transform);

    var meshesByMaterial = GetMeshesByMaterial(meshes, solids);

    return _meshByMaterialConverter.Convert(meshesByMaterial);
  }

  private static Dictionary<IRevitElementId, List<IRevitMesh>> GetMeshesByMaterial(
    List<IRevitMesh> meshes,
    List<IRevitSolid> solids
  )
  {
    var meshesByMaterial = new Dictionary<IRevitElementId, List<IRevitMesh>>();
    foreach (var mesh in meshes)
    {
      var materialId = mesh.MaterialElementId;
      if (!meshesByMaterial.TryGetValue(materialId, out List<IRevitMesh>? value))
      {
        value = new List<IRevitMesh>();
        meshesByMaterial[materialId] = value;
      }

      value.Add(mesh);
    }

    foreach (var solid in solids)
    {
      foreach (IRevitFace face in solid.Faces)
      {
        var materialId = face.MaterialElementId;
        if (!meshesByMaterial.TryGetValue(materialId, out List<IRevitMesh>? value))
        {
          value = new List<IRevitMesh>();
          meshesByMaterial[materialId] = value;
        }

        value.Add(face.Triangulate());
      }
    }

    return meshesByMaterial;
  }

  [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
  private (List<IRevitSolid>, List<IRevitMesh>) GetSolidsAndMeshesFromElement(
    IRevitElement element,
    IRevitOptions? options,
    IRevitTransform? transform = null
  )
  {
    //options = ViewSpecificOptions ?? options ?? new Options() { DetailLevel = DetailLevelSetting };
    options ??= _revitOptionsFactory.Create(RevitViewDetailLevel.Fine);

    IRevitGeometryElement geom;
    try
    {
      geom = element.GetGeometry(options);
    }
    // POC: should we be trying to continue?
    catch (Exception)
    {
      options.ComputeReferences = false;
      geom = element.GetGeometry(options);
    }

    var solids = new List<IRevitSolid>();
    var meshes = new List<IRevitMesh>();

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
    IRevitElement element,
    List<IRevitSolid> solids,
    List<IRevitMesh> meshes,
    IRevitGeometryElement geom,
    IRevitTransform? inverseTransform = null
  )
  {
    var topLevelSolidsCount = 0;
    var topLevelMeshesCount = 0;
    var topLevelGeomElementCount = 0;
    var topLevelGeomInstanceCount = 0;
    bool hasSymbolGeometry = false;

    foreach (IRevitGeometryObject geomObj in geom)
    {
      // POC: switch could possibly become factory and IIndex<,> pattern and move conversions to
      // separate IComeConversionInterfaces
      var solid = geomObj.ToSolid();
      if (solid is not null)
      {
        // skip invalid solid
        if (
          solid.Faces.Count == 0
          || Math.Abs(solid.SurfaceArea) == 0
          || IsSkippableGraphicStyle(solid.GraphicsStyleId, element.Document)
        )
        {
          continue;
        }

        if (inverseTransform != null)
        {
          topLevelSolidsCount++;
          solid = _revitSolidUtils.CreateTransformed(solid, inverseTransform);
        }

        solids.Add(solid);
      }
      else
      {
        var mesh = geomObj.ToMesh();
        if (mesh is not null)
        {
          if (IsSkippableGraphicStyle(mesh.GraphicsStyleId, element.Document))
          {
            continue;
          }

          if (inverseTransform != null)
          {
            topLevelMeshesCount++;
            mesh = mesh.GetTransformed(inverseTransform);
          }

          meshes.Add(mesh);
        }
        else
        {
          var instance = geomObj.ToGeometryInstance();
          if (instance is not null)
          {
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
          }
          else
          {
            var geometryElement = geomObj.ToGeometryElement();
            if (geometryElement is not null)
            {
              if (inverseTransform != null)
              {
                topLevelGeomElementCount++;
              }

              SortGeometry(element, solids, meshes, geometryElement);
            }
          }
        }
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
    IRevitElement element,
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
  private readonly Dictionary<string, IRevitGraphicsStyle> _graphicStyleCache = new();

  /// <summary>
  /// Exclude light source cones and potentially other geometries by their graphic style
  /// </summary>
  private bool IsSkippableGraphicStyle(IRevitElementId id, IRevitDocument doc)
  {
    if (!_graphicStyleCache.ContainsKey(id.ToString()))
    {
      _graphicStyleCache.Add(id.ToString(), doc.GetElement(id).NotNull().ToGraphicsStyle().NotNull());
    }

    var graphicStyle = _graphicStyleCache[id.ToString()];

    if (
      graphicStyle != null
      && graphicStyle.GraphicsStyleCategory.Id.IntegerValue == (int)RevitBuiltInCategory.OST_LightingFixtureSource
    )
    {
      return true;
    }

    return false;
  }
}
