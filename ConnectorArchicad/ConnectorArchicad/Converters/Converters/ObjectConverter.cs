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

    #endregion

    #region --- Functions ---

    static List<Mesh> GetDisplayValue(Base element)
    {
      List<Mesh> meshes = null;
      var m = element["displayValue"] ?? element["@displayValue"];
      if (m is List<Mesh> meshList)
        meshes = meshList;
      else if (m is List<object> objectList)
        meshes = objectList.Cast<Mesh>().ToList();
      return meshes;
    }

    static List<string> TraverseDefinitions(Base element, Transform baseTransform, Transform transform, Dictionary<string, Transform> transformMatrixById, Dictionary<string, Mesh> transformedMeshById)
    {
      // calculate commulative transforms
      Transform commulativeTansform = baseTransform * transform;
      commulativeTansform.id = Utilities.hashString(baseTransform.id + transform.id);
      transformMatrixById.TryAdd(commulativeTansform.id, commulativeTansform);
      
      // get the geometry
      List<Mesh> meshes = null;
      if (element is Mesh mesh)
        meshes = new List<Mesh>() { mesh };
      else
        meshes = GetDisplayValue(element);

      // in case no instance-level geometry, get it from definition
      Base definition = null;
      if (meshes == null)
      {
        definition = (Base)(element["definition"] ?? element["@definition"]);
        if (definition != null)
          meshes = GetDisplayValue(definition);
      }

      // transform meshes
      var meshesIds = meshes?.ConvertAll(mesh => Utilities.hashString(commulativeTansform.id + mesh.id));
      meshes = meshes?.Select(mesh => {
        var transformedMeshId = Utilities.hashString(commulativeTansform.id + mesh.id);
        if (!transformedMeshById.TryGetValue(transformedMeshId, out Mesh transformedMesh))
        {
          transformedMesh = (Mesh)mesh.ShallowCopy();
          transformedMesh.Transform (commulativeTansform);
          transformedMesh.id = transformedMeshId;
          transformedMeshById.Add(transformedMeshId, transformedMesh);
        }
        return transformedMesh;
      }).ToList();

      // continue traversal via "elements" of definition
      if (definition != null)
      { 
        var subElements = definition["elements"] ?? definition["@elements"];
        var subElementList = subElements is List<Base> baseList ? baseList : (subElements is List<object> objList ? objList.Cast<Base>().ToList() : null);
        subElementList?.ForEach(subElement =>
        {
          var subElementTransform = (Transform)(subElement["transform"]) ?? new Transform();
          meshesIds.AddRange(TraverseDefinitions(subElement, commulativeTansform, subElementTransform, transformMatrixById, transformedMeshById));
        });
      }

      return meshesIds;
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
          Transform baseTransform = new Transform();
          baseTransform.id = "1530eda076784bfaa61728b060ed8d43";
          Transform newTransform = new Transform();
          newTransform.id = "0cecc0af9be7465b958d736618817315";

          meshIdHashes = TraverseDefinitions(element, baseTransform, newTransform, transformMatrixById, transformedMeshById);

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
