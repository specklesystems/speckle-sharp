using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archicad.Communication;
using Archicad.Model;
using DynamicData;
using Objects;
using Objects.BuiltElements.Archicad;
using Objects.Geometry;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

namespace Archicad.Converters
{
  public sealed class Room : IConverter
  {
    public Type Type => typeof(Objects.BuiltElements.Room);

    public async Task<List<ApplicationObject>> ConvertToArchicad(IEnumerable<TraversalContext> elements, CancellationToken token)
    {
      var rooms = new List<Objects.BuiltElements.Archicad.ArchicadRoom>();
      foreach (var tc in elements)
      {
        switch (tc.current)
        {
          case Objects.BuiltElements.Archicad.ArchicadRoom archiRoom:
            rooms.Add(archiRoom);
            break;
          case Objects.BuiltElements.Room room:
            Objects.BuiltElements.Archicad.ArchicadRoom newRoom = new Objects.BuiltElements.Archicad.ArchicadRoom
            {
              id = room.id,
              applicationId = room.applicationId,
              shape = Utils.PolycurvesToElementShape(room.outline, room.voids),
              name = room.name,
              number = room.number,
              basePoint = (room.basePoint != null) ? Utils.ScaleToNative(room.basePoint) : new Point()
            };

            rooms.Add(newRoom);

            break;
        }
      }

      var result = await AsyncCommandProcessor.Execute(new Communication.Commands.CreateRoom(rooms), token);

      return result is null ? new List<ApplicationObject>() : result.ToList();
    }

    public async Task<List<Base>> ConvertToSpeckle(IEnumerable<Model.ElementModelData> elements,
      CancellationToken token)
    {
      var elementModels = elements as ElementModelData[] ?? elements.ToArray();
      IEnumerable<Objects.BuiltElements.Archicad.ArchicadRoom> data =
        await AsyncCommandProcessor.Execute(
          new Communication.Commands.GetRoomData(elementModels.Select(e => e.applicationId)),
          token);
      if (data is null)
      {
        return new List<Base>();
      }

      List<Base> rooms = new List<Base>();
      foreach (Objects.BuiltElements.Archicad.ArchicadRoom room in data)
      {
        room.displayValue =
          Operations.ModelConverter.MeshesToSpeckle(elementModels.First(e => e.applicationId == room.applicationId)
            .model);
        room.outline = Utils.PolycurveToSpeckle(room.shape.contourPolyline);
        if (room.shape.holePolylines?.Count > 0)
          room.voids = new List<ICurve>(room.shape.holePolylines.Select(Utils.PolycurveToSpeckle));
        rooms.Add(room);
      }

      return rooms;
    }
  }
}
