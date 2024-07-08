using System.Diagnostics;
using Rhino;
using Rhino.DocObjects;
using Speckle.Core.Models;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Speckle.Connectors.DUI.Models.Card.SendFilter;
using Speckle.Connectors.Rhino7.HostApp;
using Speckle.Connectors.Utils.Builders;
using Speckle.Connectors.Utils.Caching;
using Speckle.Connectors.Utils.Conversion;
using Speckle.Connectors.Utils.Instances;
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
  private readonly IInstanceObjectsManager<RhinoObject, List<string>> _instanceObjectsManager;
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;
  private readonly RhinoLayerManager _layerManager;

  public RhinoRootObjectBuilder(
    IUnitOfWorkFactory unitOfWorkFactory,
    ISendConversionCache sendConversionCache,
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    RhinoLayerManager layerManager,
    IInstanceObjectsManager<RhinoObject, List<string>> instanceObjectsManager
  )
  {
    _unitOfWorkFactory = unitOfWorkFactory;
    _sendConversionCache = sendConversionCache;
    _contextStack = contextStack;
    _layerManager = layerManager;
    _instanceObjectsManager = instanceObjectsManager;
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

    var rootObjectCollection = new Collection { name = _contextStack.Current.Document.Name ?? "Unnamed document" };
    int count = 0;

    Dictionary<int, Collection> layerCollectionCache = new();

    var (atomicObjects, instanceProxies, instanceDefinitionProxies) = _instanceObjectsManager.UnpackSelection(
      rhinoObjects
    );

    // POC: we should formalise this, sooner or later - or somehow fix it a bit more
    rootObjectCollection["instanceDefinitionProxies"] = instanceDefinitionProxies; // this won't work re traversal on receive

    // POC: Handle blocks.
    List<SendConversionResult> results = new(rhinoObjects.Count);
    var cacheHitCount = 0;
    foreach (RhinoObject rhinoObject in atomicObjects)
    {
      cancellationToken.ThrowIfCancellationRequested();
      // RhinoDoc.ActiveDoc.Layers
      var layer = _contextStack.Current.Document.Layers[rhinoObject.Attributes.LayerIndex];

      var collectionHost = _layerManager.GetHostObjectCollection(layer, rootObjectCollection);
      var applicationId = rhinoObject.Id.ToString();

      try
      {
        // get from cache or convert:
        // What we actually do here is check if the object has been previously converted AND has not changed.
        // If that's the case, we insert in the host collection just its object reference which has been saved from the prior conversion.
        Base converted;
        if (rhinoObject is InstanceObject)
        {
          converted = instanceProxies[applicationId];
        }
        else if (_sendConversionCache.TryGetValue(sendInfo.ProjectId, applicationId, out ObjectReference value))
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
}
