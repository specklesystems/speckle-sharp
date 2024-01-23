using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archicad.Communication;
using Archicad.Model;
using Archicad.Operations;
using Objects.Geometry;
using Objects.Other;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

namespace Archicad.Converters;

public sealed class Object : IConverter
{
  #region --- Properties ---

  public Type Type => null;

  private const string _cumulativeTransformKey = "cumulativeTransformId";

  #endregion

  #region --- Functions ---

  static List<Mesh> GetMesh(Base element)
  {
    List<Mesh> meshes = new();
    if (element is Mesh mesh)
    {
      meshes.Add(mesh);
    }

    return meshes;
  }

  public sealed class ArchicadGeometryCollectorContext : TraversalContext<ArchicadGeometryCollectorContext>
  {
    public string cumulativeTransformKey { get; set; }

    public ArchicadGeometryCollectorContext(
      Base current,
      string? propName = null,
      ArchicadGeometryCollectorContext? parent = default
    )
      : base(current, propName, parent) { }
  }

  public sealed class ArchicadGeometryCollector : GraphTraversal<ArchicadGeometryCollectorContext>
  {
    public ArchicadGeometryCollector(params ITraversalRule[] traversalRule)
      : base(traversalRule) { }

    protected override ArchicadGeometryCollectorContext NewContext(
      Base current,
      string? propName,
      ArchicadGeometryCollectorContext? parent
    )
    {
      return new ArchicadGeometryCollectorContext(current, propName, parent);
    }
  }

  public static ArchicadGeometryCollector CreateArchicadGeometryCollectorFunc(Base root)
  {
    IEnumerable<string> AllAliases(Base @base)
    {
      List<string> membersToTraverse = new();

      // hosted elements traversals
      {
        // #1: via the "elements" field of definition classes, but don't traverse the "elements" field of the root (it is traversed by the main element traversal)
        if (root != @base)
        {
          membersToTraverse.AddRange(DefaultTraversal.elementsPropAliases);
        }

        // #2: BlockInstance elements could be also in geometry field
        membersToTraverse.AddRange(DefaultTraversal.geometryPropAliases);
      }

      // instance <-> definition traversal
      {
        membersToTraverse.AddRange(DefaultTraversal.definitionPropAliases);
      }

      // geometry traversals
      {
        // #1: visiting the elements in "displayValue" field
        membersToTraverse.AddRange(DefaultTraversal.displayValuePropAliases);

        // #2: visiting the elements in "geometry" field
        membersToTraverse.AddRange(DefaultTraversal.geometryPropAliases); // already added before
      }

      return membersToTraverse;
    }

    var traversalRule = TraversalRule.NewTraversalRule().When(_ => true).ContinueTraversing(AllAliases);

    return new ArchicadGeometryCollector(traversalRule);
  }

  private static TraversalContext StoreTransformationMatrix(
    ArchicadGeometryCollectorContext tc,
    Dictionary<string, Transform> transformMatrixById
  )
  {
    if (tc.parent != null)
    {
      TraversalContext root = tc.parent;
      while (root.parent != null)
      {
        root = root.parent;
      }

      // transform appleid only elements via the "definition" property (not via "elements" property)
      // and root elements transform is skipped, because it will be added on GDL level
      var currentTransform =
        (
          tc.parent.current != root.current
          && (tc.parent.current["transform"] is Transform)
          && DefaultTraversal.definitionPropAliases.Contains(tc.propName)
        )
          ? (Transform)(tc.parent.current["transform"])
          : new Transform();

      string parentCumulativeTransformId = (tc.parent as ArchicadGeometryCollectorContext).cumulativeTransformKey;
      string cumulativeTransformId = Utilities.HashString(parentCumulativeTransformId + currentTransform.id);
      tc.cumulativeTransformKey = cumulativeTransformId;
      transformMatrixById.TryAdd(
        cumulativeTransformId,
        transformMatrixById[parentCumulativeTransformId] * currentTransform
      );
    }
    else
    {
      tc.cumulativeTransformKey = "";
      transformMatrixById.TryAdd("", new Transform());
    }
    return tc;
  }

  private static List<string> Store(
    TraversalContext tc,
    Dictionary<string, Transform> transformMatrixById,
    Dictionary<string, Mesh> transformedMeshById
  )
  {
    var meshes = GetMesh(tc.current);

    return meshes
      .Select(mesh =>
      {
        string cumulativeTransformId = (tc as ArchicadGeometryCollectorContext).cumulativeTransformKey;
        var transformedMeshId = Utilities.HashString(cumulativeTransformId + mesh.id);
        if (!transformedMeshById.TryGetValue(transformedMeshId, out Mesh transformedMesh))
        {
          transformedMesh = (Mesh)mesh.ShallowCopy();
          transformedMesh.Transform(transformMatrixById[cumulativeTransformId]);
          transformedMesh.id = transformedMeshId;
          transformedMeshById.Add(transformedMeshId, transformedMesh);
        }
        return transformedMeshId;
      })
      .ToList();
  }

  public async Task<List<ApplicationObject>> ConvertToArchicad(
    IEnumerable<TraversalContext> elements,
    CancellationToken token
  )
  {
    var archicadObjects = new List<Archicad.ArchicadObject>();
    var meshModels = new List<MeshModel>();
    var transformMatrixById = new Dictionary<string, Transform>();
    var transformedMeshById = new Dictionary<string, Mesh>();

    var context = Archicad.Helpers.Timer.Context.Peek;
    using (
      context?.cumulativeTimer?.Begin(ConnectorArchicad.Properties.OperationNameTemplates.ConvertToNative, "Object")
    )
    {
      foreach (var tc in elements)
      {
        token.ThrowIfCancellationRequested();

        var element = tc.current;

        // base point
        var basePoint = new Point(0, 0, 0);
        // var specialKeys = element.GetMembers();
        var transform = (Transform)(element["transform"]) ?? new Transform();

        // todo Speckle schema
        //if (specialKeys.ContainsKey("basePoint"))
        //  basePoint = (Point)element["basePoint"];

        List<string> meshIdHashes;
        {
          ArchicadGeometryCollector collector = CreateArchicadGeometryCollectorFunc(element);
          meshIdHashes = collector
            .Traverse(element)
            .Select(tc => StoreTransformationMatrix(tc, transformMatrixById))
            .SelectMany(tc => Store(tc, transformMatrixById, transformedMeshById))
            .ToList();

          if (meshIdHashes == null)
          {
            continue;
          }
        }

        // if the same geometry representation is not used before
        if (!archicadObjects.Any(archicadObject => archicadObject.modelIds.SequenceEqual(meshIdHashes)))
        {
          var meshes = meshIdHashes.ConvertAll(meshIdHash => transformedMeshById[meshIdHash]);
          var meshModel = ModelConverter.MeshToNative(meshes);
          meshModels.Add(meshModel);
        }

        var newObject = new Archicad.ArchicadObject
        {
          id = element.id,
          applicationId = element.applicationId,
          pos = Utils.ScaleToNative(basePoint),
          transform = new Objects.Other.Transform(transform.ConvertToUnits(Units.Meters), Units.Meters),
          modelIds = meshIdHashes,
          level = element["level"] as Objects.BuiltElements.Archicad.ArchicadLevel,
          classifications = element["classifications"] as List<Objects.BuiltElements.Archicad.Classification>
        };

        archicadObjects.Add(newObject);
      }
    }

    IEnumerable<ApplicationObject> result;
    result = await AsyncCommandProcessor.Execute(
      new Communication.Commands.CreateObject(archicadObjects, meshModels),
      token
    );
    return result is null ? new List<ApplicationObject>() : result.ToList();
  }

  public async Task<List<Base>> ConvertToSpeckle(
    IEnumerable<Model.ElementModelData> elements,
    CancellationToken token,
    ConversionOptions state
  )
  {
    // Objects not stored on the server
    return new List<Base>();
  }

  #endregion
}
