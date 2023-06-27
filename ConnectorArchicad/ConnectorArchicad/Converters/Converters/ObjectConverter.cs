using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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

namespace Archicad.Converters
{
  public sealed class Object : IConverter
  {
    #region --- Properties ---

    public Type Type => null;

    private const string _cumulativeTransformKey = "cumulativeTransformId";

    #endregion

    #region --- Functions ---

    static List<Mesh> GetMesh (Base element)
    {
      List<Mesh> meshes = new List<Mesh>();
      if (element is Mesh mesh)
        meshes.Add (mesh);

      return meshes;
    }

    public sealed class ArchicadDefinitionTraversalContext : TraversalContext<ArchicadDefinitionTraversalContext>
    {
      public string cumulativeTransformKey { get; set; }

      public ArchicadDefinitionTraversalContext(Base current, string? propName = null, ArchicadDefinitionTraversalContext? parent = default)
        : base(current, propName, parent)
      {
      }
    }

    public sealed class ArchicadDefinitionTraversal : GraphTraversal<ArchicadDefinitionTraversalContext>
    {
      public ArchicadDefinitionTraversal(params ITraversalRule[] traversalRule) : base(traversalRule) { }

      protected override ArchicadDefinitionTraversalContext NewContext(Base current, string? propName, ArchicadDefinitionTraversalContext? parent)
      {
        return new ArchicadDefinitionTraversalContext(current, propName, parent);
      }
    }

    public static ArchicadDefinitionTraversal CreateArchicadDefinitionTraverseFunc()
    {
      // hosted elements traversal #1: via the elements field
      var elementsTraversal = TraversalRule
        .NewTraversalRule()
        .When(DefaultTraversal.HasElements)
        .ContinueTraversing(DefaultTraversal.ElementsAliases);

      // hosted elements traversal #2: BlockInstance elements could be stored in geometry field
      // geometry traversal #2: visiting the elements in geometry field (Meshes)
      var geometryTraversal = TraversalRule
        .NewTraversalRule()
        .When(DefaultTraversal.HasGeometry)
        .ContinueTraversing(DefaultTraversal.GeometryAliases);

      // instance <-> definition traversal
      var definitionTraversal = TraversalRule
        .NewTraversalRule()
        .When(DefaultTraversal.HasDefiniton)
        .ContinueTraversing(DefaultTraversal.DefinitionAliases);

      // geometry traversal #1: visiting the elements in displayValue field (Meshes)
      var displayValueTraversal = TraversalRule
        .NewTraversalRule()
        .When(DefaultTraversal.HasDisplayValue)
        .ContinueTraversing(DefaultTraversal.DisplayValueAliases);

      return new ArchicadDefinitionTraversal(elementsTraversal, geometryTraversal, definitionTraversal, displayValueTraversal);
    }

    private static TraversalContext StoreTransformationMatrix(ArchicadDefinitionTraversalContext tc, Dictionary<string, Transform> transformMatrixById)
    {
      if (tc.parent != null)
      {
        var currentTransform = (Transform)(tc.current["transform"]) ?? new Transform();
        string parentCumulativeTransformId = (tc.parent as ArchicadDefinitionTraversalContext).cumulativeTransformKey;
        string cumulativeTransformId = Utilities.hashString(parentCumulativeTransformId + currentTransform.id);
        tc.cumulativeTransformKey = cumulativeTransformId;
        transformMatrixById.TryAdd(cumulativeTransformId, transformMatrixById[parentCumulativeTransformId] * currentTransform);
      }
      else
      {
        tc.cumulativeTransformKey = "";
        transformMatrixById.TryAdd("", new Transform());
      }
      return tc;
    }

    private static List<string> Store(TraversalContext tc, Dictionary<string, Transform> transformMatrixById, Dictionary<string, Mesh> transformedMeshById)
    {
      var meshes = GetMesh(tc.current);

      return meshes.Select(mesh => {
        string cumulativeTransformId = (tc as ArchicadDefinitionTraversalContext).cumulativeTransformKey;
        var transformedMeshId = Utilities.hashString(cumulativeTransformId + mesh.id);
        if (!transformedMeshById.TryGetValue(transformedMeshId, out Mesh transformedMesh))
        {
          transformedMesh = (Mesh)mesh.ShallowCopy();
          transformedMesh.Transform(transformMatrixById[cumulativeTransformId]);
          transformedMesh.id = transformedMeshId;
          transformedMeshById.Add(transformedMeshId, transformedMesh);
        }
        return transformedMeshId;
      }).ToList();
    }


    public async Task<List<ApplicationObject>> ConvertToArchicad(IEnumerable<TraversalContext> elements, CancellationToken token)
    {
      var archicadObjects = new List<Archicad.ArchicadObject>();
      var meshModels = new List<MeshModel>();
      var transformMatrixById = new Dictionary<string, Transform>();
      var transformedMeshById = new Dictionary<string, Mesh>();

      var context = Archicad.Helpers.Timer.Context.Peek;
      using (context?.cumulativeTimer?.Begin(ConnectorArchicad.Properties.OperationNameTemplates.ConvertToNative, "Object"))
      {
        ArchicadDefinitionTraversal traversal = CreateArchicadDefinitionTraverseFunc();

        foreach (var tc in elements)
        {
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
            meshIdHashes = traversal.Traverse(element)
              .Select(tc => StoreTransformationMatrix(tc, transformMatrixById))
              .SelectMany(tc => Store(tc, transformMatrixById, transformedMeshById))
              .ToList();

            if (meshIdHashes == null)
              continue;
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
            modelIds = meshIdHashes
          };

          archicadObjects.Add(newObject);
        }
      }

      IEnumerable<ApplicationObject> result;
      result = await AsyncCommandProcessor.Execute(new Communication.Commands.CreateObject(archicadObjects, meshModels), token);
      return result is null ? new List<ApplicationObject>() : result.ToList();
    }

    public async Task<List<Base>> ConvertToSpeckle(IEnumerable<Model.ElementModelData> elements, CancellationToken token)
    {
      // Objects not stored on the server
      return new List<Base>();
    }

    #endregion
  }
}
