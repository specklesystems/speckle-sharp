using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archicad.Communication;
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
      var directShapes = elements.OfType<Objects.BuiltElements.Archicad.DirectShape>();

      var elementModelDatas = (from directShape in directShapes
        let polygons = directShape.displayValue
        where polygons is not null select new Model.ElementModelData
        {
          elementId = directShape.ElementId,
            model = ModelConverter.MeshToNative(polygons)
        }).ToList();

      var result = await AsyncCommandProcessor.Execute(new Communication.Commands.CreateDirectShapes(elementModelDatas), token);
      return result is null ? new List<string>() : result.ToList();
    }

    public Task<List<Base>> ConvertToSpeckle(IEnumerable<Model.ElementModelData> elements, CancellationToken token)
    {
      return Task.FromResult(new List<Base>(elements.Select(e =>
        new Objects.BuiltElements.Archicad.DirectShape(e.elementId, ModelConverter.MeshToSpeckle(e.model)))));
    }

    #endregion
  }
}
