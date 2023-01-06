using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archicad.Communication;
using Archicad.Model;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Archicad.Converters
{
  public sealed class Column : IConverter
  {
    public Type Type => typeof(Objects.BuiltElements.Column);

    public async Task<List<string>> ConvertToArchicad(IEnumerable<Base> elements, CancellationToken token)
    {
      var columns = new List<Objects.BuiltElements.Archicad.ArchicadColumn>();
      foreach (var el in elements)
      {
        switch (el)
        {
          case Objects.BuiltElements.Archicad.ArchicadColumn archicadColumn:
            columns.Add(archicadColumn);
            break;
          case Objects.BuiltElements.Column column:
            var baseLine = (Line)column.baseLine;
            var newColumn = new Objects.BuiltElements.Archicad.ArchicadColumn(
              Utils.ScaleToNative(baseLine.start),
              Math.Abs(baseLine.end.z - baseLine.start.z)
              );
            columns.Add(newColumn);
            break;
        }
      }

      var result = await AsyncCommandProcessor.Execute(new Communication.Commands.CreateColumn(columns), token);
      return result is null ? new List<string>() : result.ToList();
    }

    public async Task<List<Base>> ConvertToSpeckle(IEnumerable<Model.ElementModelData> elements,
      CancellationToken token)
    {
      var elementModels = elements as ElementModelData[] ?? elements.ToArray();
      IEnumerable<Objects.BuiltElements.Archicad.ArchicadColumn> data =
        await AsyncCommandProcessor.Execute(
          new Communication.Commands.GetColumnData(elementModels.Select(e => e.applicationId)),
          token);
      if (data is null)
      {
        return new List<Base>();
      }

      List<Base> columns = new List<Base>();
      foreach (Objects.BuiltElements.Archicad.ArchicadColumn column in data)
      {
        column.displayValue =
          Operations.ModelConverter.MeshesToSpeckle(elementModels.First(e => e.applicationId == column.applicationId)
            .model);
        //column.baseLine = ...
        columns.Add(column);
      }

      return columns;
    }
  }
}
