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

namespace Archicad.Converters
{
  public sealed class Object : IConverter
  {
    #region --- Properties ---

    public Type Type => null;

    private const string _cumulativeTransformKey = "cumulativeTransformId";

    #endregion

    #region --- Functions ---

    static List<Mesh> GetDisplayValue(Base element)
    {
      return DefaultTraversal.displayValueAliases.Concat(DefaultTraversal.geometryAliases)
        .SelectMany(m => {
          if (element[m] is IEnumerable<object> meshes)
            return meshes.Where(mesh => mesh is Mesh).Cast<Mesh>();
          return new List<Mesh>();
          }
        ).ToList();
    }

    private static TraversalContext StoreTransformationMatrix(TraversalContext tc, Dictionary<string, Transform> transformMatrixById)
    {
      if (tc.parent != null)
      {
        var currentTransform = (Transform)(tc.current["transform"]) ?? new Transform();
        string parentCumulativeTransformId = (string)tc.parent.UserData.GetValueOrDefault(_cumulativeTransformKey, "");
        string cumulativeTransformId = Utilities.hashString(parentCumulativeTransformId + currentTransform.id);
        tc.UserData[_cumulativeTransformKey] = cumulativeTransformId;
        transformMatrixById.TryAdd(cumulativeTransformId, transformMatrixById[parentCumulativeTransformId] * currentTransform);
      }
      else
      {
        tc.UserData[_cumulativeTransformKey] = "";
        transformMatrixById.TryAdd("", new Transform());
      }
      return tc;
    }

    private static List<string> Store(TraversalContext tc, Dictionary<string, Transform> transformMatrixById, Dictionary<string, Mesh> transformedMeshById)
    {
      var meshes = GetDisplayValue(tc.current);
      return meshes.Select(mesh => {
        string cumulativeTransformId = (string)tc.UserData[_cumulativeTransformKey];
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
          meshIdHashes = DefaultTraversal.CreateDefinitionTraverseFunc().Traverse(element)
            .Select(tc => StoreTransformationMatrix(tc, transformMatrixById))
            .Where(tc => DefaultTraversal.HasDisplayValue(tc.current) || DefaultTraversal.HasGeometry (tc.current))
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

      var result = await AsyncCommandProcessor.Execute(new Communication.Commands.CreateObject(archicadObjects, meshModels), token);
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
