using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archicad.Communication;
using Objects;
using Speckle.Core.Models;
using Speckle.Core.Kits;
using Speckle.Core.Models.GraphTraversal;

namespace Archicad.Converters
{
  public sealed class Floor : IConverter
  {
    public Type Type => typeof(Objects.BuiltElements.Floor);

    public async Task<List<ApplicationObject>> ConvertToArchicad(
      IEnumerable<TraversalContext> elements,
      CancellationToken token
    )
    {
      var floors = new List<Objects.BuiltElements.Archicad.ArchicadFloor>();

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
            case Objects.BuiltElements.Archicad.ArchicadFloor archiFloor:
              floors.Add(archiFloor);
              break;
            case Objects.BuiltElements.Floor floor:

              Objects.BuiltElements.Archicad.ArchicadFloor newFloor = new Objects.BuiltElements.Archicad.ArchicadFloor
              {
                id = floor.id,
                applicationId = floor.applicationId,
                archicadLevel = Archicad.Converters.Utils.ConvertLevel(floor.level),
                shape = Utils.PolycurvesToElementShape(floor.outline, floor.voids)
              };

              floors.Add(newFloor);
              break;
          }
        }
      }

      IEnumerable<ApplicationObject> result;
      result = await AsyncCommandProcessor.Execute(new Communication.Commands.CreateFloor(floors), token);

      return result is null ? new List<ApplicationObject>() : result.ToList();
      ;
    }

    public async Task<List<Base>> ConvertToSpeckle(
      IEnumerable<Model.ElementModelData> elements,
      CancellationToken token
    )
    {
      Speckle.Newtonsoft.Json.Linq.JArray jArray = await AsyncCommandProcessor.Execute(
        new Communication.Commands.GetFloorData(elements.Select(e => e.applicationId)),
        token
      );

      var floors = new List<Base>();
      if (jArray is null)
        return floors;

      foreach (Speckle.Newtonsoft.Json.Linq.JToken jToken in jArray)
      {
        // convert between DTOs
        Objects.BuiltElements.Archicad.ArchicadFloor slab =
          Archicad.Converters.Utils.ConvertDTOs<Objects.BuiltElements.Archicad.ArchicadFloor>(jToken);

        slab.units = Units.Meters;
        slab.displayValue = Operations.ModelConverter.MeshesToSpeckle(
          elements.First(e => e.applicationId == slab.applicationId).model
        );
        slab.outline = Utils.PolycurveToSpeckle(slab.shape.contourPolyline);
        if (slab.shape.holePolylines?.Count > 0)
          slab.voids = new List<ICurve>(slab.shape.holePolylines.Select(Utils.PolycurveToSpeckle));
        floors.Add(slab);
      }

      return floors;
    }
  }
}
