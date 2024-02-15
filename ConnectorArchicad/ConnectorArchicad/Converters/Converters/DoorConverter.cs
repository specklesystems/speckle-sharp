using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archicad.Communication;
using Archicad.Model;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

namespace Archicad.Converters;

public sealed class Door : IConverter
{
  public Type Type => typeof(Objects.BuiltElements.Archicad.ArchicadDoor);

  public async Task<List<ApplicationObject>> ConvertToArchicad(
    IEnumerable<TraversalContext> elements,
    CancellationToken token
  )
  {
    var doors = new List<Objects.BuiltElements.Archicad.ArchicadDoor>();

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
          case Objects.BuiltElements.Archicad.ArchicadDoor archicadDoor:
            archicadDoor.parentApplicationId = tc.parent.current.id;
            Archicad.Converters.Utils.ConvertToArchicadDTOs<Objects.BuiltElements.Archicad.ArchicadDoor>(archicadDoor);
            doors.Add(archicadDoor);
            break;
          //case Objects.BuiltElements.Opening window:
          //  var baseLine = (Line)wall.baseLine;
          //  var newWall = new Objects.BuiltElements.Archicad.ArchicadDoor(Utils.ScaleToNative(baseLine.start),
          //    Utils.ScaleToNative(baseLine.end), Utils.ScaleToNative(wall.height, wall.units));
          //  if (el is RevitWall revitWall)
          //    newWall.flipped = revitWall.flipped;
          //  walls.Add(newWall);
          //  break;
        }
      }
    }

    IEnumerable<ApplicationObject> result;
    result = await AsyncCommandProcessor.Execute(new Communication.Commands.CreateDoor(doors), token);

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
      new Communication.Commands.GetDoorData(
        elementModels.Select(e => e.applicationId),
        conversionOptions.SendProperties,
        conversionOptions.SendListingParameters
      ),
      token
    );

    List<Base> openings = new();
    if (jArray is null)
    {
      return openings;
    }

    var context = Archicad.Helpers.Timer.Context.Peek;
    using (
      context?.cumulativeTimer?.Begin(ConnectorArchicad.Properties.OperationNameTemplates.ConvertToSpeckle, Type.Name)
    )
    {
      foreach (Speckle.Newtonsoft.Json.Linq.JToken jToken in jArray)
      {
        Objects.BuiltElements.Archicad.ArchicadDoor door =
          Archicad.Converters.Utils.ConvertToSpeckleDTOs<Objects.BuiltElements.Archicad.ArchicadDoor>(jToken);
        door.displayValue = Operations.ModelConverter.MeshesToSpeckle(
          elementModels.First(e => e.applicationId == door.applicationId).model
        );
        openings.Add(door);
      }
    }

    return openings;
  }
}
