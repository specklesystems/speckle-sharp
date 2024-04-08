using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Speckle.Connectors.Utils.Builders;
using Speckle.Converters.Common;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;

namespace Speckle.Connectors.Rhino7.Operations.Receive;

public class RhinoHostObjectBuilder : IHostObjectBuilder
{
  private readonly ISpeckleConverterToHost _toHostConverter;
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public RhinoHostObjectBuilder(
    ISpeckleConverterToHost toHostConverter,
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack
  )
  {
    _toHostConverter = toHostConverter;
    _contextStack = contextStack;
  }

  public IEnumerable<string> Build(
    Base rootObject,
    string projectName,
    string modelName,
    Action<string, double?>? onOperationProgressed,
    CancellationToken cancellationToken
  )
  {
    var baseLayerName = $"Project {projectName}: Model {modelName}";

    var objectsToConvert = rootObject.TraverseWithPath(obj => obj is not Collection);

    var convertedIds = BakeObjects(objectsToConvert, baseLayerName, onOperationProgressed, cancellationToken);

    _contextStack.Current.Document.Views.Redraw();

    return convertedIds;
  }

  private List<string> BakeObjects(
    IEnumerable<(List<string>, Base)> objects,
    string baseLayerName,
    Action<string, double?>? onOperationProgressed,
    CancellationToken cancellationToken
  )
  {
    // LET'S FUCK AROUND AND FIND OUT
    var rootLayerName = baseLayerName;
    // POC: This Find method was flagged as obsolete and I found no obvious way to work around it.
    // Silencing it for now but we should find a way to fix this.
#pragma warning disable CS0618 // Type or member is obsolete
    var rootLayerIndex = _contextStack.Current.Document.Layers.Find(rootLayerName, true);
#pragma warning restore CS0618 // Type or member is obsolete

    if (rootLayerIndex >= 0)
    {
      foreach (var layer in _contextStack.Current.Document.Layers[rootLayerIndex].GetChildren())
      {
        _contextStack.Current.Document.Layers.Purge(layer.Index, false);
      }
    }

    var cache = new Dictionary<string, int>();
    rootLayerIndex = _contextStack.Current.Document.Layers.Add(new Layer { Name = rootLayerName });
    cache.Add(rootLayerName, rootLayerIndex);

    var newObjectIds = new List<string>();
    var count = 0;
    var listObjects = objects.ToList();
    foreach ((List<string> path, Base baseObj) in objects)
    {
      cancellationToken.ThrowIfCancellationRequested();

      var fullLayerName = string.Join("::", path);
      var layerIndex = cache.TryGetValue(fullLayerName, out int value)
        ? value
        : GetAndCreateLayerFromPath(path, rootLayerName, cache);

      onOperationProgressed?.Invoke("Converting & creating objects", (double)++count / listObjects.Count);

      var converted = _toHostConverter.Convert(baseObj);
      if (converted is GeometryBase newObject)
      {
        var newObjectGuid = _contextStack.Current.Document.Objects.Add(
          newObject,
          new ObjectAttributes { LayerIndex = layerIndex }
        );
        newObjectIds.Add(newObjectGuid.ToString());
      }
      // POC:  else something weird happened? a block maybe? also, blocks are treated like $$$ now tbh so i won't dive into them
    }

    return newObjectIds;
  }

  private int GetAndCreateLayerFromPath(List<string> path, string baseLayerName, Dictionary<string, int> cache)
  {
    var currentLayerName = baseLayerName;
    RhinoDoc currentDocument = _contextStack.Current.Document;

    var previousLayer = currentDocument.Layers.FindName(currentLayerName);
    foreach (var layerName in path)
    {
      currentLayerName = baseLayerName + Layer.PathSeparator + layerName;
      currentLayerName = currentLayerName.Replace("{", "").Replace("}", ""); // Rhino specific cleanup for gh (see RemoveInvalidRhinoChars)
      if (cache.TryGetValue(currentLayerName, out int value))
      {
        previousLayer = currentDocument.Layers.FindIndex(value);
        continue;
      }
      var cleanNewLayerName = layerName.Replace("{", "").Replace("}", "");
      var newLayer = new Layer { Name = cleanNewLayerName, ParentLayerId = previousLayer.Id };
      var index = currentDocument.Layers.Add(newLayer);
      cache.Add(currentLayerName, index);
      previousLayer = currentDocument.Layers.FindIndex(index); // note we need to get the correct id out, hence why we're double calling this
    }
    return previousLayer.Index;
  }
}
