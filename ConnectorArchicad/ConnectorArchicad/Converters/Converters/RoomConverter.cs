using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archicad.Communication;
using Archicad.Model;
using Objects;
using Objects.BuiltElements.Archicad;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Archicad.Converters
{
  public sealed class Room : IConverter
  {
    public Type Type => typeof(Objects.BuiltElements.Room);

    public async Task<List<string>> ConvertToArchicad(IEnumerable<Base> elements, CancellationToken token)
    {
      var rooms = new List<Objects.BuiltElements.Archicad.ArchicadRoom>();
      foreach (var el in elements)
      {
        switch (el)
        {
          case Objects.BuiltElements.Archicad.ArchicadRoom archiRoom:
            rooms.Add(archiRoom);
            break;
          case Objects.BuiltElements.Room room:
            rooms.Add(new Objects.BuiltElements.Archicad.ArchicadRoom
            {
              shape = Utils.PolycurvesToElementShape(room.outline, room.voids),
              name = room.name,
              number = room.number,
              basePoint = Utils.ScaleToNative(room.basePoint)
            });
            break;
        }
      }

      var result = await AsyncCommandProcessor.Execute(new Communication.Commands.CreateRoom(rooms), token);

      return result is null ? new List<string>() : result.ToList();
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
