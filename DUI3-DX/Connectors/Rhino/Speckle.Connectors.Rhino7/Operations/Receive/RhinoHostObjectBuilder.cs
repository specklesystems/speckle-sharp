using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Speckle.Connectors.Utils.Builders;
using Speckle.Converters.Common;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;
using Speckle.Core.Models.GraphTraversal;

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
    // POC: This is where the top level base-layer name is set. Could be abstracted or injected in the context?
    var baseLayerName = $"Project {projectName}: Model {modelName}";

    // POC: This is the traversal from the original DUI3
    var objectsToConvert = rootObject
      .TraverseWithPath(obj => obj is not Collection)
      .Where(obj => obj.Item2 is not Collection);

    // POC: This is the new proposed traversal
    var newTraversalObjectsToConvert = DefaultTraversal
      .CreateTraversalFunc()
      .Traverse(rootObject)
      .Where(obj => obj.Current is not Collection)
      .Select(ctx => (GetLayerPath(ctx), ctx.Current));

    var convertedIds = BakeObjects(
      newTraversalObjectsToConvert, // POC: Both traversal IEnumerables can be swapped here to see the different behaviour in Rhino
      baseLayerName,
      onOperationProgressed,
      cancellationToken
    );

    _contextStack.Current.Document.Views.Redraw();

    return convertedIds;
  }

  // POC: Potentially refactor out into an IObjectBaker.
  private List<string> BakeObjects(
    IEnumerable<(string[], Base)> objects,
    string baseLayerName,
    Action<string, double?>? onOperationProgressed,
    CancellationToken cancellationToken
  )
  {
    RhinoDoc doc = _contextStack.Current.Document;
    var rootLayerIndex = _contextStack.Current.Document.Layers.Find(Guid.Empty, baseLayerName, RhinoMath.UnsetIntIndex);

    // POC: We could move this out into a separate service for testing and re-use.
    // Cleans up any previously received objects
    if (rootLayerIndex != RhinoMath.UnsetIntIndex)
    {
      Layer documentLayer = doc.Layers[rootLayerIndex];
      Layer[]? childLayers = documentLayer.GetChildren();
      if (childLayers != null)
      {
        doc.Views.RedrawEnabled = false;
        foreach (var layer in childLayers)
        {
          var purgeSuccess = doc.Layers.Purge(layer.Index, true);
          if (!purgeSuccess)
          {
            Console.WriteLine($"Failed to purge layer: {layer}");
          }
        }
        doc.Views.RedrawEnabled = true;
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

    doc.Views.RedrawEnabled = false;
    foreach ((string[] path, Base baseObj) in objects)
    {
      try
      {
        cancellationToken.ThrowIfCancellationRequested();

        var fullLayerName = string.Join(Layer.PathSeparator, path);
        var layerIndex = cache.TryGetValue(fullLayerName, out int value)
          ? value
          : GetAndCreateLayerFromPath(path, baseLayerName, cache);

        onOperationProgressed?.Invoke("Converting & creating objects", (double)++count / listObjects.Count);

        var result = _toHostConverter.Convert(baseObj);

        var conversionIds = HandleConversionResult(result, baseObj, layerIndex);
        newObjectIds.AddRange(conversionIds);
      }
      catch (OperationCanceledException)
      {
        throw;
      }
      catch (Exception e) when (!e.IsFatal())
      {
        conversionExceptions.Add(e);
      }
    }
    doc.Views.RedrawEnabled = true;

    if (conversionExceptions.Count != 0)
    {
      throw new AggregateException("Some conversions failed. Please check inner exceptions.", conversionExceptions);
    }

    return newObjectIds;
  }

  private IReadOnlyList<string> HandleConversionResult(object conversionResult, Base originalObject, int layerIndex)
  {
    var doc = _contextStack.Current.Document;
    List<string> newObjectIds = new();
    switch (conversionResult)
    {
      case IEnumerable<GeometryBase> list:
      {
        Group group = BakeObjectsAsGroup(originalObject.id, list, layerIndex);
        newObjectIds.Add(group.Id.ToString());
        break;
      }
      case GeometryBase newObject:
      {
        var newObjectGuid = doc.Objects.Add(newObject, new ObjectAttributes { LayerIndex = layerIndex });
        newObjectIds.Add(newObjectGuid.ToString());
        break;
      }
      default:
        throw new SpeckleConversionException(
          $"Unexpected result from conversion: Expected {nameof(GeometryBase)} but instead got {conversionResult.GetType().Name}"
        );
    }

    return newObjectIds;
  }

  private Group BakeObjectsAsGroup(string groupName, IEnumerable<GeometryBase> list, int layerIndex)
  {
    var doc = _contextStack.Current.Document;
    var objectIds = list.Select(obj => doc.Objects.Add(obj, new ObjectAttributes { LayerIndex = layerIndex }));
    var groupIndex = _contextStack.Current.Document.Groups.Add(groupName, objectIds);
    var group = _contextStack.Current.Document.Groups.FindIndex(groupIndex);
    return group;
  }

  // POC: This is the original DUI3 function, this will grow over time as we add more conversions that are missing, so it should be refactored out into an ILayerManager or some sort of service.
  private int GetAndCreateLayerFromPath(string[] path, string baseLayerName, Dictionary<string, int> cache)
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

  private string[] GetLayerPath(TraversalContext context)
  {
    string[] collectionBasedPath = context.GetAscendantOfType<Collection>().Select(c => c.name).ToArray();
    string[] reverseOrderPath = collectionBasedPath.Any() ? collectionBasedPath : context.GetPropertyPath().ToArray();
    return reverseOrderPath.Reverse().ToArray();
  }
}
