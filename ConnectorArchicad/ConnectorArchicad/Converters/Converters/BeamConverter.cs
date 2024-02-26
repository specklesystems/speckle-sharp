using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archicad.Communication;
using Archicad.Model;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

namespace Archicad.Converters;

public sealed class Beam : IConverter
{
  public Type Type => typeof(Objects.BuiltElements.Beam);

  public async Task<List<ApplicationObject>> ConvertToArchicad(
    IEnumerable<TraversalContext> elements,
    CancellationToken token
  )
  {
    var beams = new List<Objects.BuiltElements.Archicad.ArchicadBeam>();

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
          case Objects.BuiltElements.Archicad.ArchicadBeam archiBeam:
            Archicad.Converters.Utils.ConvertToArchicadDTOs<Objects.BuiltElements.Archicad.ArchicadBeam>(archiBeam);
            beams.Add(archiBeam);
            break;
          case Objects.BuiltElements.Beam beam:

            // upgrade (if not Archicad beam): Objects.BuiltElements.Beam --> Objects.BuiltElements.Archicad.ArchicadBeam
            {
              if (beam.baseLine is Line baseLine)
              {
                beams.Add(
                  new Objects.BuiltElements.Archicad.ArchicadBeam
                  {
                    id = beam.id,
                    applicationId = beam.applicationId,
                    archicadLevel = Archicad.Converters.Utils.ConvertLevel(beam.level),
                    begC = Utils.ScaleToNative(baseLine.start),
                    endC = Utils.ScaleToNative(baseLine.end)
                  }
                );
              }
            }

            break;
        }
      }
    }

    IEnumerable<ApplicationObject> result;
    result = await AsyncCommandProcessor.Execute(new Communication.Commands.CreateBeam(beams), token);

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
      new Communication.Commands.GetBeamData(
        elementModels.Select(e => e.applicationId),
        conversionOptions.SendProperties,
        conversionOptions.SendListingParameters
      ),
      token
    );

    var beams = new List<Base>();
    if (jArray is null)
    {
      return beams;
    }

    var context = Archicad.Helpers.Timer.Context.Peek;
    using (
      context?.cumulativeTimer?.Begin(ConnectorArchicad.Properties.OperationNameTemplates.ConvertToSpeckle, Type.Name)
    )
    {
      foreach (Speckle.Newtonsoft.Json.Linq.JToken jToken in jArray)
      {
        // convert between DTOs
        Objects.BuiltElements.Archicad.ArchicadBeam beam =
          Archicad.Converters.Utils.ConvertToSpeckleDTOs<Objects.BuiltElements.Archicad.ArchicadBeam>(jToken);

        // downgrade (always): Objects.BuiltElements.Archicad.ArchicadBeam --> Objects.BuiltElements.Beam
        {
          beam.units = Units.Meters;
          beam.displayValue = Operations.ModelConverter.MeshesToSpeckle(
            elementModels.First(e => e.applicationId == beam.applicationId).model
          );
          beam.baseLine = new Line(beam.begC, beam.endC);
        }

        beams.Add(beam);
      }
    }

    return beams;
  }
}
