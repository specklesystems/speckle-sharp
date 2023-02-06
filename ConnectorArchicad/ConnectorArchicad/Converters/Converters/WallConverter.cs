using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archicad.Communication;
using Archicad.Model;
using Objects.BuiltElements.Archicad;
using Objects.BuiltElements.Revit;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Archicad.Converters
{
  public sealed class Wall : IConverter
  {
    public Type Type => typeof(Objects.BuiltElements.Wall);

    public async Task<List<string>> ConvertToArchicad(IEnumerable<Base> elements, CancellationToken token)
    {
      var walls = new List<Objects.BuiltElements.Archicad.ArchicadWall>();
      foreach (var el in elements)
      {
        switch (el)
        {
          case Objects.BuiltElements.Archicad.ArchicadWall archiWall:
            walls.Add(archiWall);
            break;
          case Objects.BuiltElements.Wall wall:
            var baseLine = (Line)wall.baseLine;
            var newWall = new Objects.BuiltElements.Archicad.ArchicadWall(Utils.ScaleToNative(baseLine.start),
              Utils.ScaleToNative(baseLine.end), Utils.ScaleToNative(wall.height, wall.units));
            if (el is RevitWall revitWall)
              newWall.flipped = revitWall.flipped;
            walls.Add(newWall);
            break;
        }
      }

      var result = await AsyncCommandProcessor.Execute(new Communication.Commands.CreateWall(walls), token);

      return result is null ? new List<string>() : result.ToList();
    }

    public async Task<List<Base>> ConvertToSpeckle(IEnumerable<Model.ElementModelData> elements,
      CancellationToken token)
    {
      List<Base> walls = new List<Base>();
      var elementModels = elements as ElementModelData[] ?? elements.ToArray();

      IEnumerable<Objects.BuiltElements.Archicad.ArchicadWall> data =
        await AsyncCommandProcessor.Execute(
          new Communication.Commands.GetWallData(elementModels.Select(e => e.applicationId)),
          token);

      if (data is null)
        return walls;

      foreach (Objects.BuiltElements.Archicad.ArchicadWall wall in data)
      {
        wall.displayValue =
          Operations.ModelConverter.MeshesToSpeckle(elementModels.First(e => e.applicationId == wall.applicationId)
            .model);
        wall.baseLine = new Line(wall.startPoint, wall.endPoint);
        walls.Add(wall);
      }

      return walls;
    }
  }
}
