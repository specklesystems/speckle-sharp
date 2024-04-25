using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.DocObjects;
using Rhino;
using Speckle.Core.Models;
using System.Threading;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Speckle.Connectors.DUI.Models.Card.SendFilter;
using Speckle.Connectors.Utils.Builders;
using Speckle.Connectors.Utils.Operations;

namespace Speckle.Connectors.Rhino7.Operations.Send;

/// <summary>
/// Stateless builder object to turn an <see cref="ISendFilter"/> into a <see cref="Base"/> object
/// </summary>
public class RootObjectBuilder : IRootObjectBuilder<RhinoObject>
{
  private readonly IUnitOfWorkFactory _unitOfWorkFactory;

  public RootObjectBuilder(IUnitOfWorkFactory unitOfWorkFactory)
  {
    _unitOfWorkFactory = unitOfWorkFactory;
  }

  public Base Build(
    IReadOnlyList<RhinoObject> objects,
    SendInfo sendInfo,
    Action<string, double?>? onOperationProgressed = null,
    CancellationToken ct = default
  )
  {
    if (!objects.Any())
    {
      // POC: not sure if we would want to throw in here?
      throw new InvalidOperationException("No objects were found. Please update your send filter!");
    }

    Base commitObject = ConvertObjects(objects, sendInfo, onOperationProgressed, ct);

    return commitObject;
  }

  private Collection ConvertObjects(
    IReadOnlyList<RhinoObject> rhinoObjects,
    SendInfo sendInfo,
    Action<string, double?>? onOperationProgressed = null,
    CancellationToken cancellationToken = default
  )
  {
    // POC: does this feel like the right place? I am wondering if this should be called from within send/rcv?
    // begin the unit of work
    using var uow = _unitOfWorkFactory.Resolve<ISpeckleConverterToSpeckle>();
    var converter = uow.Service;

    var rootObjectCollection = new Collection { name = RhinoDoc.ActiveDoc.Name ?? "Unnamed document" };
    int count = 0;

    Dictionary<int, Collection> layerCollectionCache = new(); // POC: This seems to always start empty, so it's not caching anything out here.

    // POC: Handle blocks.
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
        // POC: We're not using the cache here yet but should once the POC is working.
        // What we actually do here is check if the object has been previously converted AND has not changed.
        // If that's the case, we insert in the host collection just its object reference which has been saved from the prior conversion.
        Base converted;
        if (
          !sendInfo.ChangedObjectIds.Contains(applicationId)
          && sendInfo.ConvertedObjects.TryGetValue(applicationId + sendInfo.ProjectId, out ObjectReference value)
        )
        {
          converted = value;
        }
        else
        {
          converted = converter.Convert(rhinoObject);
          converted.applicationId = applicationId;
        }

        // add to host
        collectionHost.elements.Add(converted);
        onOperationProgressed?.Invoke("Converting", (double)++count / rhinoObjects.Count);
      }
      // POC: Exception handling on conversion logic must be revisited after several connectors have working conversions
      catch (SpeckleConversionException e)
      {
        // POC: DO something with the exception
        Console.WriteLine(e);
      }
      catch (NotSupportedException e)
      {
        // POC: DO something with the exception
        Console.WriteLine(e);
      }

      // NOTE: useful for testing ui states, pls keep for now so we can easily uncomment
      // Thread.Sleep(550);
    }

    // 5. profit
    return rootObjectCollection;
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
