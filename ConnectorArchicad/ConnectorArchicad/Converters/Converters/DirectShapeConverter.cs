using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archicad.Communication;
using Archicad.Operations;
using Objects.Geometry;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

namespace Archicad.Converters
{
  public sealed class DirectShape : IConverter
  {
    #region --- Properties ---

    public Type Type => null;

    #endregion

    #region --- Functions ---

    public async Task<List<ApplicationObject>> ConvertToArchicad(IEnumerable<TraversalContext> elements, CancellationToken token)
    {
      var elementModelDatas = (from tc in elements
                               let directShape = tc.current
                               let polygons = (List<Mesh>)directShape["displayValue"] ??
                                 (directShape is Mesh mesh ? new List<Mesh>() { mesh } : null)
                               where polygons is not null
                               select new Model.ElementModelData
                               {
                                 applicationId = (string)directShape.applicationId ?? string.Empty,
                                 model = ModelConverter.MeshToNative(polygons)
                               }).ToList();

      var result = await AsyncCommandProcessor.Execute(new Communication.Commands.CreateDirectShapes(elementModelDatas), token);

      return result is null ? new List<ApplicationObject>() : result.ToList();
    }

    public Task<List<Base>> ConvertToSpeckle(IEnumerable<Model.ElementModelData> elements, CancellationToken token)
    {
      return Task.FromResult(new List<Base>(elements.Select(e =>
        new Objects.BuiltElements.Archicad.DirectShape(e.applicationId, ModelConverter.MeshesToSpeckle(e.model)))));
    }

    #endregion
  }
}
