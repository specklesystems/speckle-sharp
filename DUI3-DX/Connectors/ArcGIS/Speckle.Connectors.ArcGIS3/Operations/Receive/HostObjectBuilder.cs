using System.Diagnostics;
using ArcGIS.Core.Data;
using System.IO;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Mapping;
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.Utils.Builders;
using Speckle.Converters.ArcGIS3.Utils;
using Speckle.Converters.Common;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using System.Linq.Expressions;

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

    ///////////////////////// create and add Geodatabase to a project
    var fGdbPath = Directory.GetParent(Project.Current.URI).ToString();
    var fGdbName = "Speckle.gdb";

    var utils = new ArcGISProjectUtils();
    Task task = utils.addDatabaseToProject(fGdbPath, fGdbName);
    /////////////////////////

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
        // BAKE OBJECTS HERE

        QueuedTask.Run(() =>
        {
          try
          {
            var converted = converter.Convert(obj);
            if (converted is Task<string> task)
            {
              string uri = task.Result;
              objectIds.Add(obj.id);
              // TODO: get map from contextStack instead
              LayerFactory.Instance.CreateLayer(new Uri($"{fGdbPath}\\{fGdbName}\\{uri}"), MapView.Active.Map);
            }
          }
          catch (ArgumentException)
          {
            // for the layers with "invalid" names
            // doesn't do anything actually, but needs to be logged
            throw;
          }
        });

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