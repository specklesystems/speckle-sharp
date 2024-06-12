using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archicad.Communication;
using Archicad.Model;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

namespace Archicad.Converters;

public sealed class OpeningConverter : IConverter
{
  public Type Type => typeof(Objects.BuiltElements.Opening);

  public Task<List<ApplicationObject>> ConvertToArchicad(
    IEnumerable<TraversalContext> elements,
    CancellationToken token
  )
  {
    var openings = new List<Objects.BuiltElements.Archicad.ArchicadOpening>();

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
          case Objects.BuiltElements.Archicad.ArchicadOpening archicadOpening:
            archicadOpening.parentApplicationId = tc.parent.current.id;
            Archicad.Converters.Utils.ConvertToArchicadDTOs<Objects.BuiltElements.Archicad.ArchicadOpening>(
              archicadOpening
            );
            openings.Add(archicadOpening);
            break;
          case Objects.BuiltElements.Opening opening:
            try
            {
              Objects.Geometry.Vector extrusionBasePoint,
                extrusionXAxis,
                extrusionYAxis,
                extrusionZAxis;
              double width,
                height;
              Operations.ModelConverter.GetExtrusionParametersFromOutline(
                opening.outline,
                out extrusionBasePoint,
                out extrusionXAxis,
                out extrusionYAxis,
                out extrusionZAxis,
                out width,
                out height
              );
              openings.Add(
                new Objects.BuiltElements.Archicad.ArchicadOpening
                {
                  id = opening.id,
                  applicationId = opening.applicationId,
                  parentApplicationId = tc.parent.current.id,
                  extrusionGeometryBasePoint = new Objects.Geometry.Point(extrusionBasePoint),
                  extrusionGeometryXAxis = extrusionXAxis,
                  extrusionGeometryYAxis = extrusionYAxis,
                  extrusionGeometryZAxis = extrusionZAxis,
                  width = width,
                  height = height,
                  anchorIndex = 4,
                }
              );
            }
            catch (SpeckleException ex)
            {
              SpeckleLog.Logger.Error(ex.Message);
            }
            break;
        }
      }

      IEnumerable<ApplicationObject> result;
      result = AsyncCommandProcessor.Execute(new Communication.Commands.CreateOpening(openings), token).Result;

      return Task.FromResult(result is null ? new List<ApplicationObject>() : result.ToList());
    }
  }

  public async Task<List<Base>> ConvertToSpeckle(
    IEnumerable<ElementModelData> elements,
    CancellationToken token,
    ConversionOptions conversionOptions
  )
  {
    var elementModels = elements as ElementModelData[] ?? elements.ToArray();

    Speckle.Newtonsoft.Json.Linq.JArray jArray = await AsyncCommandProcessor.Execute(
      new Communication.Commands.GetOpeningData(
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
      context?.cumulativeTimer?.Begin(ConnectorArchicad.Properties.OperationNameTemplates.ConvertToNative, Type.Name)
    )
    {
      foreach (Speckle.Newtonsoft.Json.Linq.JToken jToken in jArray)
      {
        Objects.BuiltElements.Archicad.ArchicadOpening opening =
          Archicad.Converters.Utils.ConvertToSpeckleDTOs<Objects.BuiltElements.Archicad.ArchicadOpening>(jToken);
        {
          opening.outline = Operations.ModelConverter.CreateOpeningOutline(opening);
          opening.units = Units.Meters;
        }
        openings.Add(opening);
      }
    }

    return openings;
  }
}
