using System.Diagnostics;
using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Connectors.Autocad.HostApp;
using Speckle.Connectors.Autocad.HostApp.Extensions;
using Speckle.Core.Models;
using Speckle.Connectors.Utils.Builders;
using Speckle.Converters.Common;
using Speckle.Core.Logging;
using Speckle.Core.Models.GraphTraversal;

namespace Speckle.Connectors.Autocad.Operations.Receive;

public class AutocadHostObjectBuilder : IHostObjectBuilder
{
  private readonly IRootToHostConverter _converter;
  private readonly AutocadLayerManager _autocadLayerManager;
  private readonly GraphTraversal _traversalFunction;

  public AutocadHostObjectBuilder(
    IRootToHostConverter converter,
    AutocadLayerManager autocadLayerManager,
    GraphTraversal traversalFunction
  )
  {
    _converter = converter;
    _autocadLayerManager = autocadLayerManager;
    _traversalFunction = traversalFunction;
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
          string layerFullName = GetLayerPath(tc, baseLayerPrefix);

          if (uniqueLayerNames.Add(layerFullName))
          {
            _autocadLayerManager.CreateLayerOrPurge(layerFullName);
          }

          //POC: this transaction used to be called in the converter, We've moved it here to unify converter implementation
          //POC: Is this transaction 100% needed? we are already inside a transaction?
          object converted;
          using (var tr = Application.DocumentManager.CurrentDocument.Database.TransactionManager.StartTransaction())
          {
            converted = _converter.Convert(tc.Current);
            tr.Commit();
          }

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

  private string GetLayerPath(TraversalContext context, string baseLayerPrefix)
  {
    string[] collectionBasedPath = context.GetAscendantOfType<Collection>().Select(c => c.name).ToArray();
    string[] path = collectionBasedPath.Any() ? collectionBasedPath : context.GetPropertyPath().ToArray();

    return _autocadLayerManager.LayerFullName(baseLayerPrefix, string.Join("-", path)); //TODO: reverse path?
  }
}
