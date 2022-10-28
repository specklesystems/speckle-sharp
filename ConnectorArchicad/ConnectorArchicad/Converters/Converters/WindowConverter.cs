using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archicad.Communication;
using Archicad.Model;
using Objects.BuiltElements.Revit;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Archicad.Converters
{
  public sealed class Window : IConverter
  {
    public Type Type => typeof(Objects.BuiltElements.Archicad.ArchicadWindow);

    public async Task<List<string>> ConvertToArchicad(IEnumerable<Base> elements, CancellationToken token)
    {
      var walls = new List<Objects.BuiltElements.Archicad.ArchicadWindow>();
      //foreach ( var el in elements )
      //{
      //  switch ( el )
      //  {
      //    case Objects.BuiltElements.Archicad.Wall archiWall:
      //      walls.Add(archiWall);
      //      break;
      //    case Objects.BuiltElements.Wall wall:
      //      var baseLine = ( Line )wall.baseLine;
      //      var newWall = new Objects.BuiltElements.Archicad.Wall(Utils.ScaleToNative(baseLine.start),
      //        Utils.ScaleToNative(baseLine.end), Utils.ScaleToNative(wall.height, wall.units));
      //      if ( el is RevitWall revitWall )
      //        newWall.flipped = revitWall.flipped;
      //      walls.Add(newWall);
      //      break;
      //  }
      //}

      //var result = await AsyncCommandProcessor.Execute(new Communication.Commands.CreateWall(walls), token);
      
      //return result is null ? new List<string>() : result.ToList();

      return new List<string>();
    }

    public async Task<List<Base>> ConvertToSpeckle(IEnumerable<Model.ElementModelData> elements,
      CancellationToken token)
    {
      // Get subelements
      var elementModels = elements as ElementModelData[] ?? elements.ToArray();
      IEnumerable<Objects.BuiltElements.Archicad.ArchicadWindow> datas =
        await AsyncCommandProcessor.Execute(new Communication.Commands.GetWindowData(elementModels.Select(e => e.applicationId)));

      if (datas is null )
      {
        return new List<Base>();
      }

      List<Base> openings = new List<Base>();
      foreach ( Objects.BuiltElements.Archicad.ArchicadWindow subelement in datas)
      {
        subelement.displayValue =
          Operations.ModelConverter.MeshesToSpeckle(elementModels.First(e => e.applicationId == subelement.applicationId)
            .model);
        openings.Add(subelement);
      }

      return openings;
    }
  }
}
