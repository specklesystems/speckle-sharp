using System;
using System.Collections.Generic;
using Speckle.Converters.Common;
using Speckle.Core.Models;
using System.Threading;
using Autodesk.Revit.DB;
using Speckle.Converters.RevitShared.Helpers;
using System.Linq;
using Speckle.Core.Logging;

namespace Speckle.Connectors.Revit.Operations.Send;

public class RootObjectBuilder
{
  // POC: SendSelection and RevitConversionContextStack should be interfaces, former needs interfaces
  private readonly ISpeckleConverterToSpeckle _converter;
  private readonly ToSpeckleConvertedObjectsCache _convertedObjectsCache;
  private readonly RevitConversionContextStack _contextStack;
  private readonly Dictionary<string, Collection> _collectionCache;
  private readonly Collection _rootObject;

  public RootObjectBuilder(
    ISpeckleConverterToSpeckle converter,
    ToSpeckleConvertedObjectsCache convertedObjectsCache,
    RevitConversionContextStack contextStack
  )
  {
    _converter = converter;
    // POC: needs considering if this is something to add now or needs refactoring
    _convertedObjectsCache = convertedObjectsCache;
    _contextStack = contextStack;

    // Note, this class is instantiated per unit of work (aka per send operation), so we can safely initialize what we need in here.
    _collectionCache = new Dictionary<string, Collection>();
    _rootObject = new Collection()
    {
      name = _contextStack.Current.Document.Document.PathName.Split('\\').Last().Split('.').First()
    };
  }

  public Base Build(
    SendSelection sendSelection,
    Action<string, double?>? onOperationProgressed = null,
    CancellationToken ct = default
  )
  {
    var doc = _contextStack.Current.Document.Document; // POC: Document.Document is funny

    if (doc.IsFamilyDocument)
    {
      throw new SpeckleException("Family Environment documents are not supported.");
    }

    var objects = new List<Element>(); // = _contextStack.Current.Document.Document.GetElements(sendSelection.SelectedItems).ToList();

    foreach (var id in sendSelection.SelectedItems)
    {
      var el = _contextStack.Current.Document.Document.GetElement(ElementId.Parse(id));
      if (el != null)
      {
        objects.Add(el);
      }
    }

    if (objects.Count == 0)
    {
      throw new InvalidOperationException("No objects were found. Please update your send filter!");
    }

    var countProgress = 0; // because for(int i = 0; ...) loops are so last year

    foreach (Element revitElement in objects)
    {
      ct.ThrowIfCancellationRequested();

      var cat = revitElement.Category.Name;
      var path = new[] { doc.GetElement(revitElement.LevelId) is not Level level ? "No level" : level.Name, cat };
      var collection = GetAndCreateObjectHostCollection(path);

      if (_convertedObjectsCache.ContainsBaseConvertedFromId(revitElement.UniqueId))
      {
        continue;
      }

      try
      {
        var convertedElement = _converter.Convert(revitElement);
        convertedElement.applicationId = revitElement.Id.ToString();
        collection.elements.Add(convertedElement);
      }
      catch (SpeckleConversionException ex)
      {
        // POC: logging
      }

      countProgress++;
      onOperationProgressed?.Invoke("Converting", (double)countProgress / objects.Count);
    }

    return _rootObject;
  }

  /// <summary>
  /// Creates and nests collections based on the provided path within the root collection provided. This will not return a new collection each time is called, but an existing one if one is found.
  /// For example, you can use this to use (or re-use) a new collection for a path of (level, category) as it's currently implemented.
  /// </summary>
  /// <param name="path"></param>
  /// <returns></returns>
  private Collection GetAndCreateObjectHostCollection(string[] path)
  {
    string fullPathName = string.Concat(path);
    if (_collectionCache.TryGetValue(fullPathName, out Collection value))
    {
      return value;
    }

    string flatPathName = "";
    Collection previousCollection = _rootObject;

    foreach (var pathItem in path)
    {
      flatPathName += pathItem;
      Collection childCollection;
      if (_collectionCache.ContainsKey(flatPathName))
      {
        childCollection = _collectionCache[flatPathName];
      }
      else
      {
        childCollection = new Collection(pathItem, "layer");
        previousCollection.elements.Add(childCollection);
        _collectionCache[flatPathName] = childCollection;
      }

      previousCollection = childCollection;
    }

    return previousCollection;
  }
}
