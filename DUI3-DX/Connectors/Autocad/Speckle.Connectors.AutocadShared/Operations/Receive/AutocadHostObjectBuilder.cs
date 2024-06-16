using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Connectors.Autocad.HostApp;
using Speckle.Connectors.Autocad.HostApp.Extensions;
using Speckle.Connectors.Autocad.Operations.Send;
using Speckle.Core.Models;
using Speckle.Connectors.Utils.Builders;
using Speckle.Connectors.Utils.Conversion;
using Speckle.Connectors.Utils.Instances;
using Speckle.Converters.Common;
using Speckle.Core.Logging;
using Speckle.Core.Models.GraphTraversal;
using Speckle.Core.Models.Instances;

namespace Speckle.Connectors.Autocad.Operations.Receive;

/// <summary>
/// <para>Expects to be a scoped dependency per receive operation.</para>
/// </summary>
public class AutocadHostObjectBuilder : IHostObjectBuilder
{
  private readonly AutocadLayerManager _autocadLayerManager;
  private readonly IRootToHostConverter _converter;
  private readonly GraphTraversal _traversalFunction;
  private readonly HashSet<string> _uniqueLayerNames = new();
  private readonly IInstanceObjectsManager<AutocadRootObject, List<Entity>> _instanceObjectsManager;

  public AutocadHostObjectBuilder(
    IRootToHostConverter converter,
    GraphTraversal traversalFunction,
    AutocadLayerManager autocadLayerManager,
    IInstanceObjectsManager<AutocadRootObject, List<Entity>> instanceObjectsManager
  )
  {
    _converter = converter;
    _traversalFunction = traversalFunction;
    _autocadLayerManager = autocadLayerManager;
    _instanceObjectsManager = instanceObjectsManager;
  }

  public HostObjectBuilderResult Build(
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

    PreReceiveDeepClean(baseLayerPrefix);

    List<ReceiveConversionResult> results = new();
    List<string> bakedObjectIds = new();

    var objectGraph = _traversalFunction.Traverse(rootObject).Where(obj => obj.Current is not Collection);

    var instanceDefinitionProxies = (rootObject["instanceDefinitionProxies"] as List<object>)
      ?.Cast<InstanceDefinitionProxy>()
      .ToList();

    var instanceComponents = new List<(string[] path, IInstanceComponent obj)>();
    // POC: these are not captured by traversal, so we need to re-add them here
    if (instanceDefinitionProxies != null && instanceDefinitionProxies.Count > 0)
    {
      var transformed = instanceDefinitionProxies.Select(proxy => (Array.Empty<string>(), proxy as IInstanceComponent));
      instanceComponents.AddRange(transformed);
    }

    var atomicObjects = new List<(string layerName, Base obj)>();

    foreach (TraversalContext tc in objectGraph)
    {
      var layerName = GetLayerPath(tc, baseLayerPrefix);
      if (tc.Current is IInstanceComponent instanceComponent)
      {
        instanceComponents.Add((new string[] { layerName }, instanceComponent));
      }
      else
      {
        atomicObjects.Add((layerName, tc.Current));
      }
    }

    // Stage 1: Convert atomic objects
    Dictionary<string, List<Entity>> applicationIdMap = new();
    foreach (var (layerName, atomicObject) in atomicObjects)
    {
      try
      {
        var convertedObjects = ConvertObject(atomicObject, layerName).ToList();

        if (atomicObject.applicationId != null)
        {
          applicationIdMap[atomicObject.applicationId] = convertedObjects;
        }

        results.AddRange(
          convertedObjects.Select(
            e =>
              new ReceiveConversionResult(
                Status.SUCCESS,
                atomicObject,
                e.Handle.Value.ToString(),
                e.GetType().ToString()
              )
          )
        );

        bakedObjectIds.AddRange(convertedObjects.Select(e => e.Handle.Value.ToString()));
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        results.Add(new(Status.ERROR, atomicObject, null, null, ex));
      }
    }

    // Stage 2: Convert instances
    var (createdInstanceIds, consumedObjectIds, instanceConversionResults) = _instanceObjectsManager.BakeInstances(
      instanceComponents,
      applicationIdMap,
      baseLayerPrefix,
      onOperationProgressed
    );

    results.AddRange(instanceConversionResults);
    return new(bakedObjectIds, results);
  }

  private void PreReceiveDeepClean(string baseLayerPrefix)
  {
    using var transaction = Application.DocumentManager.CurrentDocument.Database.TransactionManager.StartTransaction();

    // Step 1: purge instances and instance definitions
    var instanceDefinitionsToDelete = new Dictionary<string, BlockTableRecord>();
    var modelSpaceRecord = Application.DocumentManager.CurrentDocument.Database.GetModelSpace(OpenMode.ForWrite);
    foreach (var objectId in modelSpaceRecord)
    {
      var obj = transaction.GetObject(objectId, OpenMode.ForRead) as BlockReference;
      if (obj == null)
      {
        continue;
      }

      var definition = transaction.GetObject(obj.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
      // POC: this is tightly coupled with a naming convention for definitions in the Instance object manager
      if (definition != null && definition.Name.Contains(baseLayerPrefix))
      {
        obj.UpgradeOpen();
        obj.Erase();
        instanceDefinitionsToDelete[obj.BlockTableRecord.ToString()] = definition;
      }
    }

    foreach (var def in instanceDefinitionsToDelete.Values)
    {
      def.UpgradeOpen();
      def.Erase();
    }

    // Step 2: layers and normal objects
    var layerTable = (LayerTable)
      transaction.GetObject(Application.DocumentManager.CurrentDocument.Database.LayerTableId, OpenMode.ForRead);

    foreach (var layerId in layerTable)
    {
      var layer = (LayerTableRecord)transaction.GetObject(layerId, OpenMode.ForRead);
      if (layer.Name.Contains(baseLayerPrefix))
      {
        _autocadLayerManager.CreateLayerOrPurge(layer.Name);
      }
    }
    transaction.Commit();
  }

  private IEnumerable<Entity> ConvertObject(Base obj, string layerName)
  {
    using TransactionContext transactionContext = TransactionContext.StartTransaction(
      Application.DocumentManager.MdiActiveDocument
    );

    if (_uniqueLayerNames.Add(layerName))
    {
      _autocadLayerManager.CreateLayerOrPurge(layerName);
    }

    //POC: this transaction used to be called in the converter, We've moved it here to unify converter implementation
    //POC: Is this transaction 100% needed? we are already inside a transaction?
    object converted;
    using (var tr = Application.DocumentManager.CurrentDocument.Database.TransactionManager.StartTransaction())
    {
      converted = _converter.Convert(obj);
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

      conversionResult.AppendToDb(layerName);
      yield return conversionResult;
    }
  }

  private string GetLayerPath(TraversalContext context, string baseLayerPrefix)
  {
    string[] collectionBasedPath = context.GetAscendantOfType<Collection>().Select(c => c.name).ToArray();
    string[] path = collectionBasedPath.Length != 0 ? collectionBasedPath : context.GetPropertyPath().ToArray();

    return _autocadLayerManager.GetFullLayerName(baseLayerPrefix, string.Join("-", path)); //TODO: reverse path?
  }
}
