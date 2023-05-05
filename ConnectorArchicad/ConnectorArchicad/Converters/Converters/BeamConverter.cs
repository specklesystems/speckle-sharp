using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archicad.Communication;
using Archicad.Model;
using Objects.Geometry;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

namespace Archicad.Converters
{
  public sealed class Beam : IConverter
  {
    public Type Type => typeof(Objects.BuiltElements.Beam);

    public async Task<List<ApplicationObject>> ConvertToArchicad(IEnumerable<TraversalContext> elements, CancellationToken token)
    {
      var beams = new List<Objects.BuiltElements.Archicad.ArchicadBeam>();
      foreach (var tc in elements)
      {
        switch (tc.current)
        {
          case Objects.BuiltElements.Archicad.ArchicadBeam archiBeam:
            beams.Add(archiBeam);
            break;
          case Objects.BuiltElements.Beam beam:

            // upgrade (if not Archicad beam): Objects.BuiltElements.Beam --> Objects.BuiltElements.Archicad.ArchicadBeam
            {
              var baseLine = (Line)beam.baseLine;
              Objects.BuiltElements.Archicad.ArchicadBeam newBeam = new Objects.BuiltElements.Archicad.ArchicadBeam
              {
                id = beam.id,
                applicationId = beam.applicationId,
                begC = Utils.ScaleToNative(baseLine.start),
                endC = Utils.ScaleToNative(baseLine.end)
              };

              beams.Add(newBeam);
            }
                      
            break;
        }
      }

      var result = await AsyncCommandProcessor.Execute(new Communication.Commands.CreateBeam(beams), token);

      return result is null ? new List<ApplicationObject>() : result.ToList();
    }

    public async Task<List<Base>> ConvertToSpeckle(IEnumerable<Model.ElementModelData> elements,
      CancellationToken token)
    {
      var elementModels = elements as ElementModelData[] ?? elements.ToArray();
      IEnumerable<Objects.BuiltElements.Archicad.ArchicadBeam> data =
        await AsyncCommandProcessor.Execute(
          new Communication.Commands.GetBeamData(elementModels.Select(e => e.applicationId)),
          token);
      if (data is null)
      {
        return new List<Base>();
      }

      List<Base> beams = new List<Base>();
      foreach (Objects.BuiltElements.Archicad.ArchicadBeam beam in data)
      {
        // downgrade (always): Objects.BuiltElements.Archicad.ArchicadBeam --> Objects.BuiltElements.Beam
        {
          beam.displayValue =
            Operations.ModelConverter.MeshesToSpeckle(elementModels.First(e => e.applicationId == beam.applicationId)
              .model);
          beam.baseLine = new Line(beam.begC, beam.endC);
          beams.Add(beam);
        }
      }

      return beams;
    }
  }
}
