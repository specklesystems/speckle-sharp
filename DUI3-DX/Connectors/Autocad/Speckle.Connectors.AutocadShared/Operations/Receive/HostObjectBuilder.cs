using System.Diagnostics;
using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.Autocad.HostApp;
using Speckle.Connectors.Autocad.HostApp.Extensions;
using Speckle.Core.Models;
using Speckle.Connectors.Utils.Builders;
using Speckle.Converters.Common;
using Speckle.Core.Logging;
using Speckle.Core.Models.GraphTraversal;

namespace Speckle.Connectors.Autocad.Operations.Receive;

public class HostObjectBuilder : IHostObjectBuilder
{
  private readonly IUnitOfWorkFactory _unitOfWorkFactory;
  private readonly AutocadLayerManager _autocadLayerManager;
  private readonly GraphTraversal _traversalFunction;

  public HostObjectBuilder(
    IUnitOfWorkFactory unitOfWorkFactory,
    AutocadLayerManager autocadLayerManager,
    GraphTraversal traversalFunction
  )
  {
    _unitOfWorkFactory = unitOfWorkFactory;
    _autocadLayerManager = autocadLayerManager;
    _traversalFunction = traversalFunction;
  }

  private IEnumerable<Collection> GetCollectionPath(TraversalContext context)
  {
    TraversalContext? head = context;
    do
    {
      if (head.Current is Collection c)
      {
        yield return c;
      }
      head = head.Parent;
    } while (head != null);
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
    using var uow = _unitOfWorkFactory.Resolve<ISpeckleConverterToHost>();
    var converter = uow.Service;

    // Layer filter for received commit with project and model name
    _autocadLayerManager.CreateLayerFilter(projectName, modelName);
    var traversalGraph = _traversalFunction.Traverse(rootObject).ToArray();

    string baseLayerPrefix = $"SPK-{projectName}-{modelName}-";

    HashSet<string> uniqueLayerNames = new();
    List<string> handleValues = new();
    int count = 0;

    // POC: Will be addressed to move it into AutocadContext!
    using (TransactionContext.StartTransaction(Application.DocumentManager.MdiActiveDocument))
    {
      foreach (TraversalContext tc in traversalGraph)
      {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
          string layerFullName = _autocadLayerManager.LayerFullName(
            baseLayerPrefix,
            string.Join("-", GetCollectionPath(tc).Select(c => c.name))
          );

          if (uniqueLayerNames.Add(layerFullName))
          {
            _autocadLayerManager.CreateLayerOrPurge(layerFullName);
          }

          object converted = converter.Convert(tc.Current);
          List<object> flattened = Utilities.FlattenToHostConversionResult(converted);

          foreach (Entity conversionResult in flattened.Cast<Entity>())
          {
            if (conversionResult == null)
            {
              // POC: This needed to be double checked why we check null and continue
              continue;
            }

            conversionResult.Append(layerFullName);

            handleValues.Add(conversionResult.Handle.Value.ToString());
          }

          onOperationProgressed?.Invoke("Converting", (double)++count / traversalGraph.Length);
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
