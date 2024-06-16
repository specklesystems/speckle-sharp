using System.Diagnostics;
using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Connectors.Utils.Builders;
using Speckle.Connectors.Utils.Caching;
using Speckle.Connectors.Utils.Conversion;
using Speckle.Connectors.Utils.Instances;
using Speckle.Connectors.Utils.Operations;
using Speckle.Converters.Common;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.Instances;

namespace Speckle.Connectors.Autocad.Operations.Send;

public class AutocadRootObjectBuilder : IRootObjectBuilder<AutocadRootObject>
{
  private readonly IRootToSpeckleConverter _converter;
  private readonly string[] _documentPathSeparator = { "\\" };
  private readonly ISendConversionCache _sendConversionCache;
  private readonly IInstanceObjectsManager<AutocadRootObject, List<Entity>> _instanceObjectsManager;

  public AutocadRootObjectBuilder(
    IRootToSpeckleConverter converter,
    ISendConversionCache sendConversionCache,
    IInstanceObjectsManager<AutocadRootObject, List<Entity>> instanceObjectManager
  )
  {
    _converter = converter;
    _sendConversionCache = sendConversionCache;
    _instanceObjectsManager = instanceObjectManager;
  }

  public RootObjectBuilderResult Build(
    IReadOnlyList<AutocadRootObject> objects,
    SendInfo sendInfo,
    Action<string, double?>? onOperationProgressed = null,
    CancellationToken ct = default
  )
  {
    Collection modelWithLayers =
      new()
      {
        name = Application.DocumentManager.CurrentDocument.Name // POC: https://spockle.atlassian.net/browse/CNX-9319
          .Split(_documentPathSeparator, StringSplitOptions.None)
          .Reverse()
          .First(),
        collectionType = "root"
      };

    // Cached dictionary to create Collection for autocad entity layers. We first look if collection exists. If so use it otherwise create new one for that layer.
    Dictionary<string, Collection> collectionCache = new();
    int count = 0;

    var (atomicObjects, instanceProxies, instanceDefinitionProxies) = _instanceObjectsManager.UnpackSelection(objects);
    modelWithLayers["instanceDefinitionProxies"] = instanceDefinitionProxies;

    List<SendConversionResult> results = new();
    var cacheHitCount = 0;

    foreach (var (dbObject, applicationId) in atomicObjects)
    {
      ct.ThrowIfCancellationRequested();
      try
      {
        Base converted;
        if (dbObject is BlockReference && instanceProxies.TryGetValue(applicationId, out InstanceProxy instanceProxy))
        {
          converted = instanceProxy;
        }
        else if (_sendConversionCache.TryGetValue(sendInfo.ProjectId, applicationId, out ObjectReference value))
        {
          converted = value;
          cacheHitCount++;
        }
        else
        {
          converted = _converter.Convert(dbObject);
          converted.applicationId = applicationId;
        }

        // Create and add a collection for each layer if not done so already.
        if ((dbObject as Entity)?.Layer is string layer)
        {
          if (!collectionCache.TryGetValue(layer, out Collection? collection))
          {
            collection = new Collection() { name = layer, collectionType = "layer" };
            collectionCache[layer] = collection;
            modelWithLayers.elements.Add(collectionCache[layer]);
          }

          collection.elements.Add(converted);
        }

        results.Add(new(Status.SUCCESS, applicationId, dbObject.GetType().ToString(), converted));
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        results.Add(new(Status.ERROR, applicationId, dbObject.GetType().ToString(), null, ex));
        // POC: add logging
      }

      onOperationProgressed?.Invoke("Converting", (double)++count / atomicObjects.Count);
    }

    // POC: Log would be nice, or can be removed.
    Debug.WriteLine(
      $"Cache hit count {cacheHitCount} out of {objects.Count} ({(double)cacheHitCount / objects.Count})"
    );

    return new(modelWithLayers, results);
  }
}
