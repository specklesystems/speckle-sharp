using System;
using System.Collections.Generic;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Converters.Common;
using Speckle.Core.Models;
using System.Threading;
using Autodesk.Revit.DB;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Connectors.Revit.HostApp;
using System.Linq;

namespace Speckle.Connectors.Revit.Operations.Send;

public class RootObjectBuilder
{
  private readonly SendSelection _sendSelection;
  private readonly ISpeckleConverterToSpeckle _converter;
  private readonly ToSpeckleConvertedObjectsCache _convertedObjectsCache;
  private readonly RevitConversionContextStack _contextStack;

  public RootObjectBuilder(
    ISendFilter sendFilter,
    ISpeckleConverterToSpeckle converter,
    ToSpeckleConvertedObjectsCache convertedObjectsCache,
    RevitConversionContextStack contextStack
  )
  {
    _sendSelection = new(sendFilter.GetObjectIds());
    _converter = converter;
    _convertedObjectsCache = convertedObjectsCache;
    _contextStack = contextStack;
  }

  public Base Build(Action<string, double?>? onOperationProgressed = null, CancellationToken ct = default)
  {
    List<Element> objects = _contextStack.Current.Document.Document.GetElements(_sendSelection.SelectedItems).ToList();

    Base commitObject = new();

    foreach (Element obj in objects)
    {
      if (_convertedObjectsCache.ContainsBaseConvertedFromId(obj.UniqueId))
      {
        continue;
      }

      commitObject[obj.UniqueId] = _converter.Convert(obj);
    }

    return commitObject;
  }
}
