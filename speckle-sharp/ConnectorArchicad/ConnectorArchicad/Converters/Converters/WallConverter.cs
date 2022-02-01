using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Speckle.Core.Models;

namespace Archicad.Converters
{
  public sealed class Wall : IConverter
  {
    #region --- Properties ---

    public Type Type => typeof(Objects.BuiltElements.Archicad.Wall);

    #endregion

    #region --- Functions ---

    public async Task<List<string>> ConvertToArchicad(IEnumerable<Base> elements, CancellationToken token)
    {
      var walls = elements.OfType<Objects.BuiltElements.Archicad.Wall>();
      IEnumerable<string> result = await Communication.AsyncCommandProcessor.Instance.Execute(new Communication.Commands.CreateWall(walls), token);

      return result is null ? new List<string>() : result.ToList();
    }

    public async Task<List<Base>> ConvertToSpeckle(IEnumerable<Model.ElementModelData> elements, CancellationToken token)
    {
      IEnumerable<Objects.BuiltElements.Archicad.Wall> datas = await Communication.AsyncCommandProcessor.Instance.Execute(new Communication.Commands.GetWallData(elements.Select(e => e.elementId)), token);
      if (datas is null)
      {
        return new List<Base>();
      }

      List<Base> walls = new List<Base>();
      foreach (Objects.BuiltElements.Archicad.Wall wall in datas)
      {
        wall.displayValue = Operations.ModelConverter.MeshToSpeckle(elements.First(e => e.elementId == wall.elementId).model);
        walls.Add(wall);
      }

      return walls;
    }

    #endregion
  }
}
