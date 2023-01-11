using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archicad.Communication;
using Objects;
using Speckle.Core.Models;

namespace Archicad.Converters
{
  public sealed class Floor : IConverter
  {
    public Type Type => typeof(Objects.BuiltElements.Floor);

    public async Task<List<string>> ConvertToArchicad(IEnumerable<Base> elements, CancellationToken token)
    {
      var floors = new List<Objects.BuiltElements.Archicad.ArchicadFloor>();
      foreach ( var el in elements )
      {
        switch ( el )
        {
          case Objects.BuiltElements.Archicad.ArchicadFloor archiFloor:
            floors.Add(archiFloor);
            break;
          case  Objects.BuiltElements.Floor floor:
            floors.Add(new Objects.BuiltElements.Archicad.ArchicadFloor
            {
              shape = Utils.PolycurvesToElementShape(floor.outline, floor.voids),
            });
            break;
        }
      }

      var result =
        await AsyncCommandProcessor.Execute(
          new Communication.Commands.CreateFloor(floors), token);

      return result.ToList();
    }

    public async Task<List<Base>> ConvertToSpeckle(IEnumerable<Model.ElementModelData> elements,
      CancellationToken token)
    {
      var data = await AsyncCommandProcessor.Execute(
        new Communication.Commands.GetFloorData(elements.Select(e => e.applicationId)), token);

      var floors = new List<Base>();
      foreach ( var slab in data )
      {
        slab.displayValue = Operations.ModelConverter.MeshesToSpeckle(elements
          .First(e => e.applicationId == slab.applicationId)
          .model);
        slab.outline = Utils.PolycurveToSpeckle(slab.shape.contourPolyline);
        if ( slab.shape.holePolylines?.Count > 0 )
          slab.voids = new List<ICurve>(slab.shape.holePolylines.Select(Utils.PolycurveToSpeckle));
        floors.Add(slab);
      }

      return floors;
    }
  }
}
