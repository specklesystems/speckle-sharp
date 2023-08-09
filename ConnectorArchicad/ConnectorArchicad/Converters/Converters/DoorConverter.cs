using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archicad.Communication;
using Archicad.Model;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Objects.Geometry;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

namespace Archicad.Converters
{
  public sealed class Door : IConverter
  {
    public Type Type => typeof(Objects.BuiltElements.Archicad.ArchicadDoor);

    public async Task<List<ApplicationObject>> ConvertToArchicad(IEnumerable<TraversalContext> elements, CancellationToken token)
    {
      var doors = new List<Objects.BuiltElements.Archicad.ArchicadDoor>();

      var context = Archicad.Helpers.Timer.Context.Peek;
      using (context?.cumulativeTimer?.Begin(ConnectorArchicad.Properties.OperationNameTemplates.ConvertToNative, Type.Name))
      {
        foreach (var tc in elements)
        {
          token.ThrowIfCancellationRequested();

          switch (tc.current)
          {
            case Objects.BuiltElements.Archicad.ArchicadDoor archicadDoor:
              archicadDoor.parentApplicationId = tc.parent.current.id;
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

    public async Task<List<Base>> ConvertToSpeckle(IEnumerable<Model.ElementModelData> elements,
      CancellationToken token)
    {
      var elementModels = elements as ElementModelData[] ?? elements.ToArray();
      IEnumerable<Objects.BuiltElements.Archicad.ArchicadDoor> datas =
        await AsyncCommandProcessor.Execute(new Communication.Commands.GetDoorData(elementModels.Select(e => e.applicationId)));

      if (datas is null)
      {
        return new List<Base>();
      }

      List<Base> openings = new List<Base>();
      foreach (Objects.BuiltElements.Archicad.ArchicadDoor subelement in datas)
      {
        subelement.displayValue =
          Operations.ModelConverter.MeshesToSpeckle(elementModels.First(e => e.applicationId == subelement.applicationId)
            .model);
        openings.Add(subelement);
      }

      return openings;
    }
  }
}
