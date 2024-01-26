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

public sealed class GridLineConverter : IConverter
{
  public Type Type => typeof(Archicad.GridElement);

  public async Task<List<ApplicationObject>> ConvertToArchicad(
    IEnumerable<TraversalContext> elements,
    CancellationToken token
  )
  {
    var archicadGridElements = new List<Archicad.GridElement>();

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
          case Objects.BuiltElements.GridLine grid:

            Archicad.GridElement archicadGridElement =
              new()
              {
                id = grid.id,
                applicationId = grid.applicationId,
                markerText = grid.label
              };

            if (grid.baseLine is Line)
            {
              var baseLine = (Line)grid.baseLine;
              archicadGridElement.begin = Utils.ScaleToNative(baseLine.start);
              archicadGridElement.end = Utils.ScaleToNative(baseLine.end);
              archicadGridElement.isArc = false;
            }
            else if (grid.baseLine is Arc)
            {
              var baseLine = (Arc)grid.baseLine;
              archicadGridElement.begin = Utils.ScaleToNative(baseLine.startPoint);
              archicadGridElement.end = Utils.ScaleToNative(baseLine.endPoint);
              archicadGridElement.arcAngle = baseLine.angleRadians;
              archicadGridElement.isArc = true;
            }

            archicadGridElements.Add(archicadGridElement);
            break;
        }
      }
    }

    var result = await AsyncCommandProcessor.Execute(
      new Communication.Commands.CreateGridElement(archicadGridElements),
      token
    );

    return result is null ? new List<ApplicationObject>() : result.ToList();
  }

  public async Task<List<Base>> ConvertToSpeckle(
    IEnumerable<Model.ElementModelData> elements,
    CancellationToken token,
    ConversionOptions conversionOptions
  )
  {
    var elementModels = elements as ElementModelData[] ?? elements.ToArray();
    IEnumerable<Archicad.GridElement> data = await AsyncCommandProcessor.Execute(
      new Communication.Commands.GetGridElementData(
        elementModels.Select(e => e.applicationId),
        conversionOptions.SendProperties,
        conversionOptions.SendListingParameters
      ),
      token
    );

    if (data is null)
    {
      return new List<Base>();
    }

    var context = Archicad.Helpers.Timer.Context.Peek;
    using (
      context?.cumulativeTimer?.Begin(ConnectorArchicad.Properties.OperationNameTemplates.ConvertToSpeckle, Type.Name)
    )
    {
      List<Base> gridlines = new();
      foreach (Archicad.GridElement archicadGridElement in data)
      {
        Objects.BuiltElements.GridLine speckleGridLine = new();

        // convert from Archicad to Speckle data structure
        // Speckle base properties
        speckleGridLine.id = archicadGridElement.id;
        speckleGridLine.applicationId = archicadGridElement.applicationId;
        speckleGridLine.label = archicadGridElement.markerText;
        speckleGridLine.units = Units.Meters;

        // Archicad properties
        // elementType and classifications do not exist in Objects.BuiltElements.GridLine
        //speckleGrid.elementType = archicadGridElement.elementType;
        //speckleGrid.classifications = archicadGridElement.classifications;

        if (!archicadGridElement.isArc)
        {
          speckleGridLine.baseLine = new Line(archicadGridElement.begin, archicadGridElement.end);
        }
        else
        {
          speckleGridLine.baseLine = new Arc(
            archicadGridElement.begin,
            archicadGridElement.end,
            archicadGridElement.arcAngle
          );
        }

        speckleGridLine.displayValue = Operations.ModelConverter
          .MeshesAndLinesToSpeckle(elementModels.First(e => e.applicationId == archicadGridElement.applicationId).model)
          .Cast<Base>()
          .ToList();

        gridlines.Add(speckleGridLine);
      }

      return gridlines;
    }
  }
}
