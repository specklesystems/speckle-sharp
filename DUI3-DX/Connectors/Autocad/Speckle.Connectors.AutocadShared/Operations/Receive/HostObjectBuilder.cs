using System.Diagnostics;
using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.Autocad.HostApp;
using Speckle.Connectors.Autocad.HostApp.Extensions;
using Speckle.Core.Models;
using Speckle.Connectors.Utils.Builders;
using Speckle.Converters.Common;
using Speckle.Core.Logging;
using Speckle.Core.Models.Extensions;

namespace Speckle.Connectors.Autocad.Operations.Receive;

public class HostObjectBuilder : IHostObjectBuilder
{
  private readonly IUnitOfWorkFactory<ISpeckleConverterToHost> _unitOfWorkFactory;
  private readonly AutocadLayerManager _autocadLayerManager;

  public HostObjectBuilder(
    IUnitOfWorkFactory<ISpeckleConverterToHost> unitOfWorkFactory,
    AutocadLayerManager autocadLayerManager
  )
  {
    _unitOfWorkFactory = unitOfWorkFactory;
    _autocadLayerManager = autocadLayerManager;
  }

  private List<(List<string>, Base)> GetBaseWithPath(Base commitObject, CancellationToken cancellationToken)
  {
    List<(List<string>, Base)> objectsToConvert = new();
    foreach ((List<string> objPath, Base obj) in commitObject.TraverseWithPath((obj) => obj is not Collection))
    {
      cancellationToken.ThrowIfCancellationRequested();

      if (obj is not Collection) // POC: equivalent of converter.CanConvertToNative(obj) ?
      {
        objectsToConvert.Add((objPath, obj));
      }
    }

    return objectsToConvert;
  }

  public IEnumerable<string> Build(
    Base rootObject,
    string projectName,
    string modelName,
    Action<string, double?>? onOperationProgressed,
    CancellationToken cancellationToken
  )
  {
    // Prompt the UI conversion started. Progress bar will swoosh.
    onOperationProgressed?.Invoke("Converting", null);

    // POC: does this feel like the right place? I am wondering if this should be called from within send/rcv?
    // begin the unit of work
    using var uow = _unitOfWorkFactory.Resolve();
    var converter = uow.Service;

    // Layer filter for received commit with project and model name
    _autocadLayerManager.CreateLayerFilter(projectName, modelName);
    List<(List<string>, Base)> objectsWithPath = GetBaseWithPath(rootObject, cancellationToken);
    string baseLayerPrefix = $"SPK-{projectName}-{modelName}-";

    HashSet<string> uniqueLayerNames = new();
    List<string> handleValues = new();
    int count = 0;

    // POC: Will be addressed to move it into AutocadContext!
    using (TransactionContext.StartTransaction(Application.DocumentManager.MdiActiveDocument))
    {
      foreach ((List<string> path, Base obj) in objectsWithPath)
      {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
          string layerFullName = _autocadLayerManager.LayerFullName(baseLayerPrefix, string.Join("-", path));

          if (uniqueLayerNames.Add(layerFullName))
          {
            _autocadLayerManager.CreateLayerOrPurge(layerFullName);
          }

          object converted = converter.Convert(obj);
          List<object> flattened = Core.Models.Utilities.FlattenToNativeConversionResult(converted);

          foreach (Entity conversionResult in flattened.Cast<Entity>())
          {
            if (conversionResult == null)
            {
              continue;
            }

            conversionResult.Append(layerFullName);

            handleValues.Add(conversionResult.Handle.Value.ToString());
          }

          onOperationProgressed?.Invoke("Converting", (double)++count / objectsWithPath.Count);
        }
        catch (Exception e) when (!e.IsFatal()) // DO NOT CATCH SPECIFIC STUFF, conversion errors should be recoverable
        {
          // POC: report, etc.
          Debug.WriteLine("conversion error happened.");
        }
      }
    }
    return handleValues;
  }
}
