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
  public sealed class Roof : IConverter
  {
    public Type Type => typeof(Objects.BuiltElements.Roof);

    public async Task<List<ApplicationObject>> ConvertToArchicad(IEnumerable<TraversalContext> elements, CancellationToken token)
    {
      var roofs = new List<Objects.BuiltElements.Archicad.ArchicadRoof>();
      var shells = new List<Objects.BuiltElements.Archicad.ArchicadShell>();

      var context = Archicad.Helpers.Timer.Context.Peek;
      using (context?.cumulativeTimer?.Begin(ConnectorArchicad.Properties.OperationNameTemplates.ConvertToNative, Type.Name))
      {
        foreach (var tc in elements)
        {
          token.ThrowIfCancellationRequested();

          switch (tc.current)
          {
            case Objects.BuiltElements.Archicad.ArchicadRoof archiRoof:
              roofs.Add(archiRoof);
              break;
            case Objects.BuiltElements.Archicad.ArchicadShell archiShell:
              shells.Add(archiShell);
              break;
            case Objects.BuiltElements.Roof roof:
              roofs.Add(new Objects.BuiltElements.Archicad.ArchicadRoof
              {
                id = roof.id,
                applicationId = roof.applicationId,
                shape = Utils.PolycurvesToElementShape(roof.outline, roof.voids),
              });
              break;
          }
        }
      }

      var resultRoofs = roofs.Count > 0 ? await AsyncCommandProcessor.Execute(new Communication.Commands.CreateRoof(roofs), token) : null;
      var resultShells = shells.Count > 0 ? await AsyncCommandProcessor.Execute(new Communication.Commands.CreateShell(shells), token) : null;

      var result = new List<ApplicationObject> ();
      if (resultRoofs is not null)
        result.AddRange(resultRoofs.ToList());

      if (resultShells is not null)
        result.AddRange(resultShells.ToList());

      return result is null ? new List<ApplicationObject>() : result;
    }

    public async Task<List<Base>> ConvertToSpeckle(IEnumerable<Model.ElementModelData> elements,
      CancellationToken token)
    {
      var data = await AsyncCommandProcessor.Execute(
        new Communication.Commands.GetRoofData(elements.Select(e => e.applicationId)), token);

      var Roofs = new List<Base>();
      foreach (var roof in data)
      {
        roof.displayValue = Operations.ModelConverter.MeshesToSpeckle(elements
          .First(e => e.applicationId == roof.applicationId)
          .model);

        if (roof.shape != null)
        {
          roof.outline = Utils.PolycurveToSpeckle(roof.shape.contourPolyline);
          if (roof.shape.holePolylines?.Count > 0)
            roof.voids = new List<ICurve>(roof.shape.holePolylines.Select(Utils.PolycurveToSpeckle));
        }

        Roofs.Add(roof);
      }

     var shelldData = await AsyncCommandProcessor.Execute(
        new Communication.Commands.GetShellData(elements.Select(e => e.applicationId)), token);

      var Shells = new List<Base>();
      foreach (var shell in shelldData)
      {
        shell.displayValue = Operations.ModelConverter.MeshesToSpeckle(elements
          .First(e => e.applicationId == shell.applicationId)
          .model);

        if (shell.shape != null)
        {
          shell.outline = Utils.PolycurveToSpeckle(shell.shape.contourPolyline);
          if (shell.shape.holePolylines?.Count > 0)
            shell.voids = new List<ICurve>(shell.shape.holePolylines.Select(Utils.PolycurveToSpeckle));
        }

        Shells.Add(shell);
      }

      var result = Roofs;
      result.AddRange(Shells);
      return result;
    }
  }
}
