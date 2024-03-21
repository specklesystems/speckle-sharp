using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.DocObjects;
using Rhino;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Core.Models;
using Speckle.Core.Kits;
using System.Threading;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Speckle.Connectors.DUI.Models.Card;

namespace Speckle.Connectors.Rhino7.Operations.Send;

/// <summary>
/// Stateless builder object to turn an <see cref="ISendFilter"/> into a <see cref="Base"/> object
/// </summary>
public class RootBaseObjectBuilder
{
  private readonly IScopedFactory<ISpeckleConverterToSpeckle> _converterFactory;

  public RootBaseObjectBuilder(IScopedFactory<ISpeckleConverterToSpeckle> converterFactory)
  {
    _converterFactory = converterFactory;
  }

  public Base Build(
    ISendFilter sendFilter,
    Action<string, double?>? onOperationProgressed = null,
    CancellationToken ct = default
  )
  {
    List<RhinoObject> rhinoObjects = sendFilter
      .GetObjectIds()
      .Select(id => RhinoDoc.ActiveDoc.Objects.FindId(new Guid(id)))
      .Where(obj => obj != null)
      .ToList();

    if (rhinoObjects.Count == 0)
    {
      throw new InvalidOperationException("No objects were found. Please update your send filter!");
    }

    var converter = _converterFactory.ResolveScopedInstance();

    //converter.SetContextDocument(RhinoDoc.ActiveDoc);
    Base commitObject = ConvertObjects(rhinoObjects, converter, onOperationProgressed, ct);

    return commitObject;
  }

  private Base ConvertObjects(
    List<RhinoObject> rhinoObjects,
    SenderModelCard modelCard,
    CancellationToken cancellationToken
  )
  {
    ISpeckleConverterToSpeckle converter = _converterFactory.ResolveScopedInstance();

    var rootObjectCollection = new Collection { name = RhinoDoc.ActiveDoc.Name ?? "Unnamed document" };
    int count = 0;

    Dictionary<int, Collection> layerCollectionCache = new();
    // TODO: Handle blocks.
    foreach (RhinoObject rhinoObject in rhinoObjects)
    {
      cancellationToken.ThrowIfCancellationRequested();

      // 1. get object layer
      var layer = RhinoDoc.ActiveDoc.Layers[rhinoObject.Attributes.LayerIndex];

      // 2. get or create a nested collection for it
      var collectionHost = GetHostObjectCollection(layerCollectionCache, layer, rootObjectCollection);
      var applicationId = rhinoObject.Id.ToString();

      // 3. get from cache or convert:
      // What we actually do here is check if the object has been previously converted AND has not changed.
      // If that's the case, we insert in the host collection just its object reference which has been saved from the prior conversion.
      /*Base converted;
      if (
        !modelCard.ChangedObjectIds.Contains(applicationId)
        && _convertedObjectReferences.TryGetValue(applicationId + modelCard.ProjectId, out ObjectReference value)
      )
      {
        converted = value;
      }
      else
      {
        converted = converter.ConvertToSpeckle(rhinoObject);
        converted.applicationId = applicationId;
      }*/
      try
      {
        Base converted = converter.Convert(rhinoObject);
        converted.applicationId = applicationId;

        // 4. add to host
        collectionHost.elements.Add(converted);
        _basicConnectorBinding.Commands.SetModelProgress(
          modelCard.ModelCardId,
          new ModelCardProgress { Status = "Converting", Progress = (double)++count / rhinoObjects.Count }
        );
      }
      catch (SpeckleConversionException e)
      {
        // DO something with the exception
        Console.WriteLine(e);
      }
      catch (NotSupportedException e)
      {
        // DO something with the exception
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

  private static Collection ConvertObjects(
    List<RhinoObject> rhinoObjects,
    ISpeckleConverterToSpeckle converter,
    Action<string, double?>? onOperationProgressed = null,
    CancellationToken ct = default
  )
  {
    var rootObjectCollection = new Collection { name = RhinoDoc.ActiveDoc.Name ?? "Unnamed document" };
    int count = 0;

    Dictionary<int, Collection> layerCollectionCache = new();
    // TODO: Handle blocks.
    foreach (RhinoObject rhinoObject in rhinoObjects)
    {
      ct.ThrowIfCancellationRequested();

      // 1. get object layer
      var layer = RhinoDoc.ActiveDoc.Layers[rhinoObject.Attributes.LayerIndex];

      // 2. get or create a nested collection for it
      var collectionHost = GetHostObjectCollection(layerCollectionCache, layer, rootObjectCollection);
      var applicationId = rhinoObject.Id.ToString();

      // 3. get from cache or convert:
      // What we actually do here is check if the object has been previously converted AND has not changed.
      // If that's the case, we insert in the host collection just its object reference which has been saved from the prior conversion.
      /*Base converted;
      if (
        !modelCard.ChangedObjectIds.Contains(applicationId)
        && _convertedObjectReferences.TryGetValue(applicationId + modelCard.ProjectId, out ObjectReference value)
      )
      {
        converted = value;
      }
      else
      {
        converted = converter.ConvertToSpeckle(rhinoObject);
        converted.applicationId = applicationId;
      }*/

      var converted = converter.ConvertToSpeckle(rhinoObject);
      converted.applicationId = applicationId;

      // 4. add to host
      collectionHost.elements.Add(converted);
      //_basicConnectorBinding.Commands.SetModelProgress(
      //  modelCard.ModelCardId,
      //  new ModelCardProgress { Status = "Converting", Progress = (double)++count / rhinoObjects.Count }
      //);
      onOperationProgressed?.Invoke("Converting", (double)++count / rhinoObjects.Count);

      // NOTE: useful for testing ui states, pls keep for now so we can easily uncomment
      // Thread.Sleep(550);
    }

    // 5. profit
    return rootObjectCollection;
  }
}
