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

  private bool _operationStarted;

  public RootObjectBuilder(
    SendSelection sendSelection,
    ISpeckleConverterToSpeckle converter,
    RevitContext revitContext,
    ToSpeckleConvertedObjectsCache convertedObjectsCache
  )
  {
    _sendSelection = sendSelection;
    _converter = converter;
    _revitContext = revitContext;
    _convertedObjectsCache = convertedObjectsCache;
  }

  public Base Build(
    ISendFilter sendFilter,
    Action<string, double?>? onOperationProgressed = null,
    CancellationToken ct = default
  )
  {
    if (_operationStarted)
    {
      throw new InvalidOperationException();
    }
    _operationStarted = true;

    _sendSelection.SetSelection(sendFilter.GetObjectIds());

    List<Element> objects = _revitContext.UIApplication.ActiveUIDocument.Document
      .GetElements(sendFilter.GetObjectIds())
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
