using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archicad.Communication;
using Objects;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

namespace Archicad.Converters;

public sealed class Roof : IConverter
{
  public Type Type => typeof(Objects.BuiltElements.Roof);

  public async Task<List<ApplicationObject>> ConvertToArchicad(
    IEnumerable<TraversalContext> elements,
    CancellationToken token
  )
  {
    var roofs = new List<Objects.BuiltElements.Archicad.ArchicadRoof>();
    var shells = new List<Objects.BuiltElements.Archicad.ArchicadShell>();

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
          case Objects.BuiltElements.Archicad.ArchicadRoof archiRoof:
            Archicad.Converters.Utils.ConvertToArchicadDTOs<Objects.BuiltElements.Archicad.ArchicadRoof>(archiRoof);
            roofs.Add(archiRoof);
            break;
          case Objects.BuiltElements.Archicad.ArchicadShell archiShell:
            Archicad.Converters.Utils.ConvertToArchicadDTOs<Objects.BuiltElements.Archicad.ArchicadShell>(archiShell);
            shells.Add(archiShell);
            break;
          case Objects.BuiltElements.Roof roof:

            {
              try
              {
                roofs.Add(
                  new Objects.BuiltElements.Archicad.ArchicadRoof
                  {
                    id = roof.id,
                    applicationId = roof.applicationId,
                    archicadLevel = Archicad.Converters.Utils.ConvertLevel(roof.level),
                    shape = Utils.PolycurvesToElementShape(roof.outline, roof.voids)
                  }
                );
              }
              catch (SpeckleException ex)
              {
                SpeckleLog.Logger.Error(ex, "Polycurves conversion failed.");
              }
            }
            break;
        }
      }
    }

    var resultRoofs =
      roofs.Count > 0 ? await AsyncCommandProcessor.Execute(new Communication.Commands.CreateRoof(roofs), token) : null;
    var resultShells =
      shells.Count > 0
        ? await AsyncCommandProcessor.Execute(new Communication.Commands.CreateShell(shells), token)
        : null;

    var result = new List<ApplicationObject>();
    if (resultRoofs is not null)
    {
      result.AddRange(resultRoofs.ToList());
    }

    if (resultShells is not null)
    {
      result.AddRange(resultShells.ToList());
    }

    return result is null ? new List<ApplicationObject>() : result;
  }

  public async Task<List<Base>> ConvertToSpeckle(
    IEnumerable<Model.ElementModelData> elements,
    CancellationToken token,
    ConversionOptions conversionOptions
  )
  {
    Speckle.Newtonsoft.Json.Linq.JArray jArray = await AsyncCommandProcessor.Execute(
      new Communication.Commands.GetRoofData(
        elements.Select(e => e.applicationId),
        conversionOptions.SendProperties,
        conversionOptions.SendListingParameters
      ),
      token
    );

    var roofs = new List<Base>();
    if (jArray is not null)
    {
      var context = Archicad.Helpers.Timer.Context.Peek;
      using (
        context?.cumulativeTimer?.Begin(ConnectorArchicad.Properties.OperationNameTemplates.ConvertToSpeckle, Type.Name)
      )
      {
        foreach (Speckle.Newtonsoft.Json.Linq.JToken jToken in jArray)
        {
          // convert between DTOs
          Objects.BuiltElements.Archicad.ArchicadRoof roof =
            Archicad.Converters.Utils.ConvertToSpeckleDTOs<Objects.BuiltElements.Archicad.ArchicadRoof>(jToken);

          roof.units = Units.Meters;
          roof.displayValue = Operations.ModelConverter.MeshesToSpeckle(
            elements.First(e => e.applicationId == roof.applicationId).model
          );

          if (roof.shape != null)
          {
            roof.outline = Utils.PolycurveToSpeckle(roof.shape.contourPolyline);
            if (roof.shape.holePolylines?.Count > 0)
            {
              roof.voids = new List<ICurve>(roof.shape.holePolylines.Select(Utils.PolycurveToSpeckle));
            }
          }

          roofs.Add(roof);
        }
      }
    }

    jArray = await AsyncCommandProcessor.Execute(
      new Communication.Commands.GetShellData(
        elements.Select(e => e.applicationId),
        conversionOptions.SendProperties,
        conversionOptions.SendListingParameters
      ),
      token
    );

    var shells = new List<Base>();
    if (jArray is not null)
    {
      var context = Archicad.Helpers.Timer.Context.Peek;
      using (
        context?.cumulativeTimer?.Begin(ConnectorArchicad.Properties.OperationNameTemplates.ConvertToSpeckle, Type.Name)
      )
      {
        foreach (Speckle.Newtonsoft.Json.Linq.JToken jToken in jArray)
        {
          // convert between DTOs
          Objects.BuiltElements.Archicad.ArchicadShell shell =
            Archicad.Converters.Utils.ConvertToSpeckleDTOs<Objects.BuiltElements.Archicad.ArchicadShell>(jToken);

          shell.units = Units.Meters;
          shell.displayValue = Operations.ModelConverter.MeshesToSpeckle(
            elements.First(e => e.applicationId == shell.applicationId).model
          );

          if (shell.shape != null)
          {
            shell.outline = Utils.PolycurveToSpeckle(shell.shape.contourPolyline);
            if (shell.shape.holePolylines?.Count > 0)
            {
              shell.voids = new List<ICurve>(shell.shape.holePolylines.Select(Utils.PolycurveToSpeckle));
            }
          }

          shells.Add(shell);
        }
      }
    }

    var result = roofs;
    result.AddRange(shells);
    return result;
  }
}
