using System.Diagnostics.Contracts;
using Rhino;
using Rhino.DocObjects;
using Speckle.Converters.Common;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

namespace Speckle.Connectors.Rhino7.HostApp;

/// <summary>
/// Utility class managing layer creation and/or extraction from rhino. Expects to be a scoped dependency per send or receive operation.
/// </summary>
public class RhinoLayerManager
{
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;
  private readonly Dictionary<string, int> _hostLayerCache;
  private readonly Dictionary<int, Collection> _layerCollectionCache;

  public RhinoLayerManager(IConversionContextStack<RhinoDoc, UnitSystem> contextStack)
  {
    _contextStack = contextStack;
    _hostLayerCache = new();
    _layerCollectionCache = new();
  }

  /// <summary>
  /// Creates the base layer and adds it to the cache.
  /// </summary>
  /// <param name="baseLayerName"></param>
  public void CreateBaseLayer(string baseLayerName)
  {
    var index = _contextStack.Current.Document.Layers.Add(new Layer { Name = baseLayerName });
    _hostLayerCache.Add(baseLayerName, index);
  }

  /// <summary>
  /// <para>For receive: Use this method to construct layers in the host app when receiving.</para>.
  /// </summary>
  /// <param name="path"></param>
  /// <param name="baseLayerName"></param>
  /// <returns></returns>
  public int GetAndCreateLayerFromPath(string[] path, string baseLayerName)
  {
    var fullLayerName = string.Join(Layer.PathSeparator, path);
    if (_hostLayerCache.TryGetValue(fullLayerName, out int existingLayerIndex))
    {
      return existingLayerIndex;
    }

    var currentLayerName = baseLayerName;
    RhinoDoc currentDocument = _contextStack.Current.Document;

    var previousLayer = currentDocument.Layers.FindName(currentLayerName);
    foreach (var layerName in path)
    {
      currentLayerName = baseLayerName + Layer.PathSeparator + layerName;
      currentLayerName = currentLayerName.Replace("{", "").Replace("}", ""); // Rhino specific cleanup for gh (see RemoveInvalidRhinoChars)
      if (_hostLayerCache.TryGetValue(currentLayerName, out int value))
      {
        previousLayer = currentDocument.Layers.FindIndex(value);
        continue;
      }

      var cleanNewLayerName = layerName.Replace("{", "").Replace("}", "");
      var newLayer = new Layer { Name = cleanNewLayerName, ParentLayerId = previousLayer.Id };
      var index = currentDocument.Layers.Add(newLayer);
      _hostLayerCache.Add(currentLayerName, index);
      previousLayer = currentDocument.Layers.FindIndex(index); // note we need to get the correct id out, hence why we're double calling this
    }
    return previousLayer.Index;
  }

  /// <summary>
  /// <para>For send: Use this method to construct the root commit object while converting objects.</para>
  /// <para>Returns the host collection corresponding to the provided layer. If it's the first time that it is being asked for, it will be created and stored in the root object collection.</para>
  /// </summary>
  /// <param name="layer">The layer you want the equivalent collection for.</param>
  /// <param name="rootObjectCollection">The root object that will be sent to Speckle, and will host all collections.</param>
  /// <returns></returns>
  public Collection GetHostObjectCollection(Layer layer, Collection rootObjectCollection)
  {
    if (_layerCollectionCache.TryGetValue(layer.Index, out Collection value))
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
      if (_layerCollectionCache.TryGetValue(existingLayerIndex, out Collection? collection))
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
        _layerCollectionCache[existingLayerIndex] = childCollection;
      }

      previousCollection = childCollection;

      if (index < names.Length - 1)
      {
        path += Layer.PathSeparator + names[index + 1];
      }
      index++;
    }

    _layerCollectionCache[layer.Index] = previousCollection;
    return previousCollection;
  }

  [Pure]
  public string[] GetLayerPath(TraversalContext context)
  {
    string[] collectionBasedPath = context.GetAscendantOfType<Collection>().Select(c => c.name).ToArray();
    string[] reverseOrderPath =
      collectionBasedPath.Length != 0 ? collectionBasedPath : context.GetPropertyPath().ToArray();
    return reverseOrderPath.Reverse().ToArray();
  }
}
