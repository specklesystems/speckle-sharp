using System;
using System.Collections.Generic;
using Speckle.Converters.Common;
using Speckle.Core.Models;
using System.Threading;
using Autodesk.Revit.DB;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Connectors.Revit.HostApp;
using System.Linq;
using Speckle.Connectors.DUI.Models.Card.SendFilter;

namespace Speckle.Connectors.Revit.Operations.Send;

public class RootObjectBuilder
{
  // POC: SendSelection and RevitConversionContextStack should be interfaces, former needs interfaces
  private readonly SendSelection _sendSelection;
  private readonly ISpeckleConverterToSpeckle _converter;
  private readonly ToSpeckleConvertedObjectsCache _convertedObjectsCache;
  private readonly RevitConversionContextStack _contextStack;

  public RootObjectBuilder(
    ISendFilter sendFilter,
    // POC: need to resolve where the UoW should be and whether this is with the SpeckleConverterToSpeckle or something else
    ISpeckleConverterToSpeckle converter,
    ToSpeckleConvertedObjectsCache convertedObjectsCache,
    RevitConversionContextStack contextStack
  )
  {
    _sendSelection = new(sendFilter.GetObjectIds());
    _converter = converter;
    // POC: needs considering if this is something to add now or needs refactoring
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
