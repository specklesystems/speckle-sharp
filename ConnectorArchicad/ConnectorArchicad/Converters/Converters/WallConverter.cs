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
using Speckle.Core.Models.GraphTraversal;

namespace Archicad.Converters
{
  public sealed class Wall : IConverter
  {
    public Type Type => typeof(Objects.BuiltElements.Wall);

    public async Task<List<ApplicationObject>> ConvertToArchicad(IEnumerable<TraversalContext> elements, CancellationToken token)
    {
      var walls = new List<Objects.BuiltElements.Archicad.ArchicadWall>();

      var context = Archicad.Helpers.Timer.Context.Peek;
      using (context?.cumulativeTimer?.Begin(ConnectorArchicad.Properties.OperationNameTemplates.ConvertToNative, Type.Name))
      {
        foreach (var tc in elements)
        {
          token.ThrowIfCancellationRequested();

          switch (tc.current)
          {
            case Objects.BuiltElements.Archicad.ArchicadWall archiWall:
              walls.Add(archiWall);
              break;
            case Objects.BuiltElements.Wall wall:
              var baseLine = (Line)wall.baseLine;
              Objects.BuiltElements.Archicad.ArchicadWall newWall = new Objects.BuiltElements.Archicad.ArchicadWall
              {
                id = wall.id,
                applicationId = wall.applicationId,
                startPoint = Utils.ScaleToNative(baseLine.start),
                endPoint = Utils.ScaleToNative(baseLine.end),
                height = Utils.ScaleToNative(wall.height, wall.units),
                flipped = (tc.current is RevitWall revitWall) ? revitWall.flipped : false
              };

              walls.Add(newWall);
              break;
          }
        }
      }

      var result = await AsyncCommandProcessor.Execute(new Communication.Commands.CreateWall(walls), token);

      return result is null ? new List<ApplicationObject>() : result.ToList();
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
