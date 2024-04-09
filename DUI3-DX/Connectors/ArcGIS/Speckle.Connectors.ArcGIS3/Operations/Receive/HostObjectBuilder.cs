using System.Diagnostics;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Mapping;
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.Utils.Builders;
using Speckle.Converters.Common;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;

namespace Speckle.Connectors.ArcGIS.Operations.Receive;

public class HostObjectBuilder : IHostObjectBuilder
{
  private readonly IScopedFactory<ISpeckleConverterToHost> _speckleConverterToHostFactory;

  // private readonly IConversionContextStack<Map, Unit> _contextStack;

  public HostObjectBuilder(
    IScopedFactory<ISpeckleConverterToHost> speckleConverterToHostFactory //,
  // IConversionContextStack<Map, Unit> contextStack
  )
  {
    _speckleConverterToHostFactory = speckleConverterToHostFactory;
    // _contextStack = contextStack;
  }

  public IEnumerable<string> Build(
    Base rootObject,
    string projectName,
    string modelName,
    Action<string, double?>? onOperationProgressed,
    CancellationTokenSource cts
  )
  {
    // Prompt the UI conversion started. Progress bar will swoosh.
    onOperationProgressed?.Invoke("Converting", null);

    ISpeckleConverterToHost converter = _speckleConverterToHostFactory.ResolveScopedInstance();

    // POC: This is where we will define our receive strategy, or maybe later somewhere else according to some setting pass from UI?
    IEnumerable<(List<string>, Base)> objectsWithPath = rootObject.TraverseWithPath((obj) => obj is not Collection);

    List<string> objectIds = new();
    int count = 0;
    foreach ((List<string> path, Base obj) in objectsWithPath)
    {
      if (cts.IsCancellationRequested)
      {
        throw new OperationCanceledException(cts.Token);
      }

      try
      {
        // POC: Question to Kate -> Conversion returns already baked objects?? If so we do not necessarily need below loop?
        object converted = converter.Convert(obj); // NULL right now returns Polyline, should return Feature class/Raster
        List<object> flattened = Utilities.FlattenToNativeConversionResult(converted); // not necessary

        foreach (var conversionResult in flattened)
        {
          if (conversionResult == null)
          {
            continue;
          }
        }
        onOperationProgressed?.Invoke("Converting", (double)++count / objectsWithPath.Count());
      }
      catch (Exception e) when (!e.IsFatal()) // DO NOT CATCH SPECIFIC STUFF, conversion errors should be recoverable
      {
        // POC: report, etc.
        Debug.WriteLine("conversion error happened.");
      }
    }

    return objectIds;
  }
}
