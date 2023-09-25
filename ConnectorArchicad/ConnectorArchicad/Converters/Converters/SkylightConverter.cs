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
  public sealed class Skylight : IConverter
  {
    public Type Type => typeof(Objects.BuiltElements.Archicad.ArchicadSkylight);

    public async Task<List<ApplicationObject>> ConvertToArchicad(IEnumerable<TraversalContext> elements, CancellationToken token)
    {
      var skylights = new List<Objects.BuiltElements.Archicad.ArchicadSkylight>();

      var context = Archicad.Helpers.Timer.Context.Peek;
      using (context?.cumulativeTimer?.Begin(ConnectorArchicad.Properties.OperationNameTemplates.ConvertToNative, Type.Name))
      {
        foreach (var tc in elements)
        {
          token.ThrowIfCancellationRequested();

          switch (tc.current)
          {
            case Objects.BuiltElements.Archicad.ArchicadSkylight archicadSkylight:
              archicadSkylight.parentApplicationId = tc.parent.current.id;
              skylights.Add(archicadSkylight);
              break;
              //case Objects.BuiltElements.Opening skylight:
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

      var result = await AsyncCommandProcessor.Execute(new Communication.Commands.CreateSkylight(skylights), token);

      return result is null ? new List<ApplicationObject>() : result.ToList();
    }

    public async Task<List<Base>> ConvertToSpeckle(IEnumerable<Model.ElementModelData> elements,
      CancellationToken token)
    {
      // Get subelements
      var elementModels = elements as ElementModelData[] ?? elements.ToArray();
      IEnumerable<Objects.BuiltElements.Archicad.ArchicadSkylight> datas =
        await AsyncCommandProcessor.Execute(new Communication.Commands.GetSkylightData(elementModels.Select(e => e.applicationId)));

      if (datas is null)
      {
        return new List<Base>();
      }

      List<Base> openings = new List<Base>();
      foreach (Objects.BuiltElements.Archicad.ArchicadSkylight subelement in datas)
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
