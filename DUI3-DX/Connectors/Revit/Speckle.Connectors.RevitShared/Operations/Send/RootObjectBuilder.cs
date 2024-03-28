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
  private readonly RevitContext _revitContext;
  private readonly ToSpeckleConvertedObjectsCache _convertedObjectsCache;

  public RootObjectBuilder(
    ISendFilter sendFilter,
    ISpeckleConverterToSpeckle converter,
    RevitContext revitContext,
    ToSpeckleConvertedObjectsCache convertedObjectsCache
  )
  {
    _sendSelection = new(sendFilter.GetObjectIds());
    _converter = converter;
    _revitContext = revitContext;
    _convertedObjectsCache = convertedObjectsCache;
  }

  public Base Build(Action<string, double?>? onOperationProgressed = null, CancellationToken ct = default)
  {
    List<Element> objects = _revitContext.UIApplication.ActiveUIDocument.Document
      .GetElements(_sendSelection.SelectedItems)
      .ToList();

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
