using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archicad.Communication;
using Archicad.Model;
using Archicad.Operations;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

namespace Archicad.Converters;

public sealed class DirectShape : IConverter
{
  #region --- Properties ---

  public Type Type => typeof(Objects.BuiltElements.Archicad.DirectShape);

  #endregion

  #region --- Functions ---

  public async Task<List<ApplicationObject>> ConvertToArchicad(
    IEnumerable<TraversalContext> elements,
    CancellationToken token
  )
  {
    var directShapes = new List<Objects.BuiltElements.Archicad.DirectShape>();

    var context = Archicad.Helpers.Timer.Context.Peek;
    using (
      context?.cumulativeTimer?.Begin(ConnectorArchicad.Properties.OperationNameTemplates.ConvertToNative, Type.Name)
    )
    {
      foreach (var tc in elements)
      {
        token.ThrowIfCancellationRequested();

        switch (tc.current)
        {
          case Objects.BuiltElements.Archicad.DirectShape directShape:
            // get the geometry
            MeshModel meshModel = null;

            {
              List<Mesh> meshes = null;
              var m = directShape["displayValue"] ?? directShape["@displayValue"];
              if (m is List<Mesh>)
              {
                meshes = (List<Mesh>)m;
              }
              else if (m is List<object>)
              {
                meshes = ((List<object>)m).Cast<Mesh>().ToList();
              }

              if (meshes == null)
              {
                continue;
              }

              meshModel = ModelConverter.MeshToNative(meshes);
            }

            directShape["model"] = meshModel;
            directShapes.Add(directShape);
            break;
        }
      }
    }

    IEnumerable<ApplicationObject> result;
    result = await AsyncCommandProcessor.Execute(new Communication.Commands.CreateDirectShape(directShapes), token);

    return result is null ? new List<ApplicationObject>() : result.ToList();
  }

  public async Task<List<Base>> ConvertToSpeckle(
    IEnumerable<Model.ElementModelData> elements,
    CancellationToken token,
    ConversionOptions conversionOptions
  )
  {
    var elementModels = elements as ElementModelData[] ?? elements.ToArray();

    Speckle.Newtonsoft.Json.Linq.JArray jArray = await AsyncCommandProcessor.Execute(
      new Communication.Commands.GetElementBaseData(
        elementModels.Select(e => e.applicationId),
        conversionOptions.SendProperties,
        conversionOptions.SendListingParameters
      ),
      token
    );

    var directShapes = new List<Base>();
    if (jArray is null)
    {
      return directShapes;
    }

    var context = Archicad.Helpers.Timer.Context.Peek;
    using (
      context?.cumulativeTimer?.Begin(ConnectorArchicad.Properties.OperationNameTemplates.ConvertToSpeckle, Type.Name)
    )
    {
      foreach (Speckle.Newtonsoft.Json.Linq.JToken jToken in jArray)
      {
        // convert between DTOs
        Objects.BuiltElements.Archicad.DirectShape directShape =
          Archicad.Converters.Utils.ConvertToSpeckleDTOs<Objects.BuiltElements.Archicad.DirectShape>(jToken);

        {
          directShape.units = Units.Meters;
          directShape.displayValue = Operations.ModelConverter.MeshesToSpeckle(
            elementModels.First(e => e.applicationId == directShape.applicationId).model
          );

          directShapes.Add(directShape);
        }
      }
    }

    return directShapes;
  }

  #endregion
}
