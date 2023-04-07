using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archicad.Communication;
using Objects;
using Objects.BuiltElements;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

namespace Archicad.Converters
{
  public sealed class Floor : IConverter
  {
    public Type Type => typeof(Objects.BuiltElements.Floor);

    public async Task<List<ApplicationObject>> ConvertToArchicad(IEnumerable<TraversalContext> elements, CancellationToken token)
    {
      var floors = new List<Objects.BuiltElements.Archicad.ArchicadFloor>();
      foreach (var tc in elements)
      {
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
              shape = Utils.PolycurvesToElementShape(floor.outline, floor.voids),
            };

            floors.Add(newFloor);
            break;
        }
      }

      var result =
        await AsyncCommandProcessor.Execute(
          new Communication.Commands.CreateFloor(floors), token);

      return result is null ? new List<ApplicationObject>() : result.ToList(); ;
    }

    public async Task<List<Base>> ConvertToSpeckle(IEnumerable<Model.ElementModelData> elements,
      CancellationToken token)
    {
      var data = await AsyncCommandProcessor.Execute(
        new Communication.Commands.GetFloorData(elements.Select(e => e.applicationId)), token);

      var floors = new List<Base>();
      foreach (var slab in data)
      {
        slab.displayValue = Operations.ModelConverter.MeshesToSpeckle(elements
          .First(e => e.applicationId == slab.applicationId)
          .model);
        slab.outline = Utils.PolycurveToSpeckle(slab.shape.contourPolyline);
        if (slab.shape.holePolylines?.Count > 0)
          slab.voids = new List<ICurve>(slab.shape.holePolylines.Select(Utils.PolycurveToSpeckle));
        floors.Add(slab);
      }

      return floors;
    }
  }
}
