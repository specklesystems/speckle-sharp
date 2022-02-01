using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archicad.Operations;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Archicad.Converters
{
  public sealed class DirectShape : IConverter
  {
    #region --- Properties ---

    public Type Type => null;

    #endregion

    #region --- Functions ---

    public async Task<List<string>> ConvertToArchicad(IEnumerable<Base> elements, CancellationToken token)
    {
      IEnumerable<Objects.BuiltElements.Archicad.DirectShape> directShapes = elements.OfType<Objects.BuiltElements.Archicad.DirectShape>();

      List<Model.ElementModelData> elementModelDatas = new List<Model.ElementModelData>();
      foreach (var directShape in directShapes)
      {
        Base model = directShape["Model"] as Base;
        if (model is null)
        {
          continue;
        }

        List<object> polygons = model["Polygons"] as List<object>;
        if (polygons is null)
        {
          continue;
        }

        elementModelDatas.Add(new Model.ElementModelData { elementId = directShape.ElementId, model = ModelConverter.MeshToNative(polygons.OfType<Mesh>()) });
      }

      IEnumerable<string> result = await Communication.AsyncCommandProcessor.Instance.Execute(new Communication.Commands.CreateDirectShapes(elementModelDatas), token);
      return result is null ? new List<string>() : result.ToList();
    }

    public Task<List<Base>> ConvertToSpeckle(IEnumerable<Model.ElementModelData> elements, CancellationToken token)
    {
      return Task.FromResult(new List<Base>(elements.Select(e => new Objects.BuiltElements.Archicad.DirectShape(e.elementId, ModelConverter.MeshToSpeckle(e.model)))));
    }

    #endregion
  }
}
