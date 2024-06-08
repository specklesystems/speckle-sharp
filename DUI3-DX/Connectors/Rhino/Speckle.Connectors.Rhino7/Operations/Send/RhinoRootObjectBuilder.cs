using System.Diagnostics;
using Rhino.DocObjects;
using Rhino;
using Speckle.Core.Models;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Speckle.Connectors.DUI.Models.Card.SendFilter;
using Speckle.Connectors.Utils.Builders;
using Speckle.Connectors.Utils.Caching;
using Speckle.Connectors.Utils.Conversion;
using Speckle.Connectors.Utils.Operations;
using Speckle.Core.Logging;

namespace Speckle.Connectors.Rhino7.Operations.Send;

/// <summary>
/// Stateless builder object to turn an <see cref="ISendFilter"/> into a <see cref="Base"/> object
/// </summary>
public class RhinoRootObjectBuilder : IRootObjectBuilder<RhinoObject>
{
  private readonly IUnitOfWorkFactory _unitOfWorkFactory;
  private readonly ISendConversionCache _sendConversionCache;

  public RhinoRootObjectBuilder(IUnitOfWorkFactory unitOfWorkFactory, ISendConversionCache sendConversionCache)
  {
    _unitOfWorkFactory = unitOfWorkFactory;
    _sendConversionCache = sendConversionCache;
  }

  public RootObjectBuilderResult Build(
    IReadOnlyList<RhinoObject> objects,
    SendInfo sendInfo,
    Action<string, double?>? onOperationProgressed = null,
    CancellationToken ct = default
  ) => ConvertObjects(objects, sendInfo, onOperationProgressed, ct);

  private RootObjectBuilderResult ConvertObjects(
    IReadOnlyList<RhinoObject> rhinoObjects,
    SendInfo sendInfo,
    Action<string, double?>? onOperationProgressed = null,
    CancellationToken cancellationToken = default
  )
  {
    // POC: does this feel like the right place? I am wondering if this should be called from within send/rcv?
    // begin the unit of work
    using var uow = _unitOfWorkFactory.Resolve<IRootToSpeckleConverter>();
    var converter = uow.Service;

    var rootObjectCollection = new Collection { name = RhinoDoc.ActiveDoc.Name ?? "Unnamed document" };
    int count = 0;

    Dictionary<int, Collection> layerCollectionCache = new();

    // POC: Handle blocks.
    List<SendConversionResult> results = new(rhinoObjects.Count);
    var cacheHitCount = 0;
    foreach (RhinoObject rhinoObject in rhinoObjects)
    {
      cancellationToken.ThrowIfCancellationRequested();

      // POC: This uses the ActiveDoc but it is bad practice to do so. A context object should be injected that would contain the Doc.
      var layer = RhinoDoc.ActiveDoc.Layers[rhinoObject.Attributes.LayerIndex];

      var collectionHost = GetHostObjectCollection(layerCollectionCache, layer, rootObjectCollection);
      var applicationId = rhinoObject.Id.ToString();

      try
      {
        // get from cache or convert:
        // What we actually do here is check if the object has been previously converted AND has not changed.
        // If that's the case, we insert in the host collection just its object reference which has been saved from the prior conversion.
        Base converted;

        if (_sendConversionCache.TryGetValue(sendInfo.ProjectId, applicationId, out ObjectReference value))
        {
          converted = value;
          cacheHitCount++;
        }
        else
        {
          converted = converter.Convert(rhinoObject);
          converted.applicationId = applicationId;
        }

        // add to host
        collectionHost.elements.Add(converted);

        results.Add(new(Status.SUCCESS, applicationId, rhinoObject.ObjectType.ToString(), converted));
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        results.Add(new(Status.ERROR, applicationId, rhinoObject.ObjectType.ToString(), null, ex));
      }

      onOperationProgressed?.Invoke("Converting", (double)++count / rhinoObjects.Count);

      // NOTE: useful for testing ui states, pls keep for now so we can easily uncomment
      // Thread.Sleep(550);
    }

    // POC: Log would be nice, or can be removed.
    Debug.WriteLine(
      $"Cache hit count {cacheHitCount} out of {rhinoObjects.Count} ({(double)cacheHitCount / rhinoObjects.Count})"
    );

    // 5. profit
    return new(rootObjectCollection, results);
  }

  /// <summary>
  /// Returns the host collection based on the provided layer. If it's not found, it will be created and hosted within the the rootObjectCollection.
  /// </summary>
  /// <param name="layerCollectionCache"></param>
  /// <param name="layer"></param>
  /// <param name="rootObjectCollection"></param>
  /// <returns></returns>
  private Collection GetHostObjectCollection(
    Dictionary<int, Collection> layerCollectionCache,
    Layer layer,
    Collection rootObjectCollection
  )
  {
    // POC: This entire implementation should be broken down and potentially injected in.
    if (layerCollectionCache.TryGetValue(layer.Index, out Collection value))
    {
      return value;
    }

    var names = layer.FullPath.Split(new[] { Layer.PathSeparator }, StringSplitOptions.None);
    var path = names[0];
    var index = 0;
    var previousCollection = rootObjectCollection;
    foreach (var layerName in names)
    {
      var existingLayerIndex = RhinoDoc.ActiveDoc.Layers.FindByFullPath(path, -1);
      Collection? childCollection = null;
      if (layerCollectionCache.TryGetValue(existingLayerIndex, out Collection? collection))
      {
        childCollection = collection;
      }
      else
      {
        childCollection = new Collection(layerName, "layer")
        {
          applicationId = RhinoDoc.ActiveDoc.Layers[existingLayerIndex].Id.ToString()
        };
        previousCollection.elements.Add(childCollection);
        layerCollectionCache[existingLayerIndex] = childCollection;
      }

      previousCollection = childCollection;

      if (index < names.Length - 1)
      {
        path += Layer.PathSeparator + names[index + 1];
      }
      index++;
    }

    layerCollectionCache[layer.Index] = previousCollection;
    return previousCollection;
  }
}
