using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Objects.Other;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Speckle.Connectors.Utils.Builders;
using Speckle.Converters.Common;
using Speckle.Core.Logging;
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

    var objectsToConvert = rootObject
      .TraverseWithPath(obj => obj is not Collection)
      .Where(obj => obj.Item2 is not Collection);

    var convertedIds = BakeObjects(objectsToConvert, baseLayerName, onOperationProgressed, cancellationToken);

    _contextStack.Current.Document.Views.Redraw();

    return convertedIds;
  }

  // POC: Potentially refactor out into an IObjectBaker.
  private List<string> BakeObjects(
    IEnumerable<(List<string>, Base)> objects,
    string baseLayerName,
    Action<string, double?>? onOperationProgressed,
    CancellationToken cancellationToken
  )
  {
    RhinoDoc doc = _contextStack.Current.Document;

    var rootLayerIndex = doc.Layers.Find(Guid.Empty, baseLayerName, RhinoMath.UnsetIntIndex);

    // Cleans up any previously received objects
    if (rootLayerIndex >= 0)
    {
      Layer documentLayer = doc.Layers[rootLayerIndex];
      Layer[]? childLayers = documentLayer.GetChildren();
      if (childLayers != null)
      {
        foreach (var layer in childLayers)
        {
          doc.Layers.Purge(layer.Index, false);
        }
      }
    }

    var cache = new Dictionary<string, int>();
    rootLayerIndex = doc.Layers.Add(new Layer { Name = baseLayerName });
    cache.Add(baseLayerName, rootLayerIndex);

    var newObjectIds = new List<string>();
    var count = 0;
    var listObjects = objects.ToList();

    // POC: We delay throwing conversion exceptions until the end of the conversion loop, then throw all within an aggregate exception if something happened.
    var conversionExceptions = new List<Exception>();

    foreach ((List<string> path, Base baseObj) in objects)
    {
      try
      {
        cancellationToken.ThrowIfCancellationRequested();

        var fullLayerName = string.Join("::", path);
        var layerIndex = cache.TryGetValue(fullLayerName, out int value)
          ? value
          : GetAndCreateLayerFromPath(path, baseLayerName, cache);

        onOperationProgressed?.Invoke("Converting & creating objects", (double)++count / listObjects.Count);

        var converted = _toHostConverter.Convert(baseObj);

        if (converted is GeometryBase newObject)
        {
          var newObjectGuid = doc.Objects.Add(newObject, new ObjectAttributes { LayerIndex = layerIndex });
          newObjectIds.Add(newObjectGuid.ToString());
          continue;
        }

        // POC:  else something weird happened? a block maybe? We should stop on our tracks if we reach this.
        throw new SpeckleException(
          $"Unexpected result from conversion: Expected {nameof(GeometryBase)} but instead got {converted.GetType().Name}"
        );
      }
      catch (Exception e) when (!e.IsFatal())
      {
        conversionExceptions.Add(e);
      }
    }

    if (conversionExceptions.Count != 0)
    {
      // POC: Both the message and the handling of this should be engineered taking into account error reporting in DUI becoming better.
      throw new AggregateException("Some conversions failed. Please check inner exceptions.", conversionExceptions);
    }

    return newObjectIds;
  }

  // POC: This is the original DUI3 function, this will grow over time as we add more conversions that are missing, so it should be refactored out into an ILayerManager or some sort of service.
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
