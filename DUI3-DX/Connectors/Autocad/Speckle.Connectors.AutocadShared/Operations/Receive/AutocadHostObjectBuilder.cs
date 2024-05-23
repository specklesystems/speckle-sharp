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
  private readonly AutocadLayerManager _autocadLayerManager;
  private readonly IRootToHostConverter _converter;
  private readonly GraphTraversal _traversalFunction;

  public AutocadHostObjectBuilder(
    IRootToHostConverter converter,
    GraphTraversal traversalFunction,
    AutocadLayerManager autocadLayerManager
  )
  {
    _converter = converter;
    _traversalFunction = traversalFunction;
    _autocadLayerManager = autocadLayerManager;
  }

  public IReadOnlyList<ConversionResult> Build(
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

    //TODO: make the layerManager handle \/ ?
    string baseLayerPrefix = $"SPK-{projectName}-{modelName}-";
    HashSet<string> uniqueLayerNames = new();

    List<ConversionResult> results = new();
    foreach (var tc in _traversalFunction.TraverseWithProgress(rootObject, onOperationProgressed, cancellationToken))
    {
      try
      {
        var convertedObjects = ConvertObject(tc, baseLayerPrefix, uniqueLayerNames);

        results.AddRange(convertedObjects.Select(e => new ConversionResult(tc, e, e.Handle.Value.ToString())));
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        results.Add(new(tc, ex));
      }
    }

    return results;
  }

  private IEnumerable<Entity> ConvertObject(TraversalContext tc, string baseLayerPrefix, ISet<string> uniqueLayerNames)
  {
    using TransactionContext transactionContext = TransactionContext.StartTransaction(
      Application.DocumentManager.MdiActiveDocument
    );

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

    IEnumerable<Entity?> flattened = Utilities.FlattenToHostConversionResult(converted).Cast<Entity>();

    foreach (Entity? conversionResult in flattened)
    {
      if (conversionResult == null)
      {
        // POC: This needed to be double checked why we check null and continue
        continue;
      }

      conversionResult.AppendToDb(layerFullName);

      yield return conversionResult;
    }
  }

  private string GetLayerPath(TraversalContext context, string baseLayerPrefix)
  {
    string[] collectionBasedPath = context.GetAscendantOfType<Collection>().Select(c => c.name).ToArray();
    string[] path = collectionBasedPath.Any() ? collectionBasedPath : context.GetPropertyPath().ToArray();

    return _autocadLayerManager.LayerFullName(baseLayerPrefix, string.Join("-", path)); //TODO: reverse path?
  }
}
