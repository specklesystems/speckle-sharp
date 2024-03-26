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
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

namespace Archicad.Converters;

public sealed class Wall : IConverter
{
  public Type Type => typeof(Objects.BuiltElements.Wall);

  public async Task<List<ApplicationObject>> ConvertToArchicad(
    IEnumerable<TraversalContext> elements,
    CancellationToken token
  )
  {
    var walls = new List<Objects.BuiltElements.Archicad.ArchicadWall>();

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
          case Objects.BuiltElements.Archicad.ArchicadWall archiWall:
            Archicad.Converters.Utils.ConvertToArchicadDTOs<Objects.BuiltElements.Archicad.ArchicadWall>(archiWall);
            walls.Add(archiWall);
            break;
          case Objects.BuiltElements.Wall wall:
            if (wall.baseLine is Line baseLine)
            {
              walls.Add(
                new ArchicadWall
                {
                  id = wall.id,
                  applicationId = wall.applicationId,
                  archicadLevel = Archicad.Converters.Utils.ConvertLevel(wall.level),
                  startPoint = Utils.ScaleToNative(baseLine.start),
                  endPoint = Utils.ScaleToNative(baseLine.end),
                  height = Utils.ScaleToNative(wall.height, wall.units),
                  flipped = (tc.current is RevitWall revitWall) ? revitWall.flipped : false
                }
              );
            }
            break;
        }
      }
    }

    var result = await AsyncCommandProcessor.Execute(new Communication.Commands.CreateWall(walls), token);

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
      new Communication.Commands.GetWallData(
        elementModels.Select(e => e.applicationId),
        conversionOptions.SendProperties,
        conversionOptions.SendListingParameters
      ),
      token
    );

    var walls = new List<Base>();
    if (jArray is null)
    {
      return walls;
    }

    var context = Archicad.Helpers.Timer.Context.Peek;
    using (
      context?.cumulativeTimer?.Begin(ConnectorArchicad.Properties.OperationNameTemplates.ConvertToSpeckle, Type.Name)
    )
    {
      foreach (Speckle.Newtonsoft.Json.Linq.JToken jToken in jArray)
      {
        // convert between DTOs
        Objects.BuiltElements.Archicad.ArchicadWall wall =
          Archicad.Converters.Utils.ConvertToSpeckleDTOs<Objects.BuiltElements.Archicad.ArchicadWall>(jToken);

        wall.units = Units.Meters;
        wall.displayValue = Operations.ModelConverter.MeshesToSpeckle(
          elementModels.First(e => e.applicationId == wall.applicationId).model
        );
        wall.baseLine = new Line(wall.startPoint, wall.endPoint);
        walls.Add(wall);
      }
    }

    return walls;
  }
}
