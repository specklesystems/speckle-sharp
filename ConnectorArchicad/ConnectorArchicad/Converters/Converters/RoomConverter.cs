using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archicad.Communication;
using Archicad.Model;
using Objects;
using Objects.BuiltElements.Archicad;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

namespace Archicad.Converters;

public sealed class Room : IConverter
{
  public Type Type => typeof(Archicad.Room);

  public async Task<List<ApplicationObject>> ConvertToArchicad(
    IEnumerable<TraversalContext> elements,
    CancellationToken token
  )
  {
    var rooms = new List<Archicad.Room>();

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
          case Objects.BuiltElements.Archicad.ArchicadRoom speckleRoom:

            {
              Archicad.Room archicadRoom =
                new()
                {
                  // convert from Speckle to Archicad data structure
                  // Speckle base properties
                  id = speckleRoom.id,
                  applicationId = speckleRoom.applicationId,
                  // Speckle general properties
                  name = speckleRoom.name,
                  number = speckleRoom.number,
                  // Archicad properties
                  elementType = speckleRoom.elementType,
                  classifications = speckleRoom.classifications,
                  level = speckleRoom.archicadLevel,
                  height = speckleRoom.height,
                  shape = speckleRoom.shape
                };

              rooms.Add(archicadRoom);
            }
            break;
          case Objects.BuiltElements.Room speckleRoom:

            {
              try
              {
                rooms.Add(
                  new Archicad.Room
                  {
                    // Speckle base properties
                    id = speckleRoom.id,
                    applicationId = speckleRoom.applicationId,
                    // Speckle general properties
                    name = speckleRoom.name,
                    number = speckleRoom.number,
                    // Archicad properties
                    level = Archicad.Converters.Utils.ConvertLevel(speckleRoom.level),
                    shape = Utils.PolycurvesToElementShape(speckleRoom.outline, speckleRoom.voids)
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

    var result = await AsyncCommandProcessor.Execute(new Communication.Commands.CreateRoom(rooms), token);

    return result is null ? new List<ApplicationObject>() : result.ToList();
  }

  public async Task<List<Base>> ConvertToSpeckle(
    IEnumerable<Model.ElementModelData> elements,
    CancellationToken token,
    ConversionOptions conversionOptions
  )
  {
    var elementModels = elements as ElementModelData[] ?? elements.ToArray();
    IEnumerable<Archicad.Room> data = await AsyncCommandProcessor.Execute(
      new Communication.Commands.GetRoomData(
        elementModels.Select(e => e.applicationId),
        conversionOptions.SendProperties,
        conversionOptions.SendListingParameters
      ),
      token
    );

    var rooms = new List<Base>();
    if (data is null)
    {
      return rooms;
    }

    var context = Archicad.Helpers.Timer.Context.Peek;
    using (
      context?.cumulativeTimer?.Begin(ConnectorArchicad.Properties.OperationNameTemplates.ConvertToSpeckle, Type.Name)
    )
    {
      foreach (Archicad.Room archicadRoom in data)
      {
        Objects.BuiltElements.Archicad.ArchicadRoom speckleRoom = new();

        // convert from Archicad to Speckle data structure
        // Speckle base properties
        speckleRoom.id = archicadRoom.id;
        speckleRoom.applicationId = archicadRoom.applicationId;
        speckleRoom.displayValue = Operations.ModelConverter.MeshesToSpeckle(
          elementModels.First(e => e.applicationId == archicadRoom.applicationId).model
        );
        speckleRoom.units = Units.Meters;

        // Archicad properties
        speckleRoom.elementType = archicadRoom.elementType;
        speckleRoom.classifications = archicadRoom.classifications;
        speckleRoom.elementProperties = PropertyGroup.ToBase(archicadRoom.elementProperties);
        speckleRoom.componentProperties = ComponentProperties.ToBase(archicadRoom.componentProperties);
        speckleRoom.archicadLevel = archicadRoom.level;
        speckleRoom.height = archicadRoom.height ?? .0;
        speckleRoom.shape = archicadRoom.shape;

        // downdgrade
        speckleRoom.name = archicadRoom.name;
        speckleRoom.number = archicadRoom.number;
        speckleRoom.area = archicadRoom.area ?? .0;
        speckleRoom.volume = archicadRoom.volume ?? .0;

        ElementShape.Polyline polyLine = archicadRoom.shape.contourPolyline;
        Polycurve polycurve = Utils.PolycurveToSpeckle(polyLine);
        speckleRoom.outline = polycurve;
        if (archicadRoom.shape.holePolylines?.Count > 0)
        {
          speckleRoom.voids = new List<ICurve>(archicadRoom.shape.holePolylines.Select(Utils.PolycurveToSpeckle));
        }

        // calculate base point
        speckleRoom.basePoint = archicadRoom.basePoint;

        rooms.Add(speckleRoom);
      }
    }

    return rooms;
  }
}
