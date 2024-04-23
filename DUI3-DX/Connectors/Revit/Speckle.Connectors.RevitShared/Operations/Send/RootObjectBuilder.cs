using System;
using System.Collections.Generic;
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
  // POC: SendSelection and RevitConversionContextStack should be interfaces, former needs interfaces
  private readonly ISpeckleConverterToSpeckle _converter;
  private readonly ToSpeckleConvertedObjectsCache _convertedObjectsCache;
  private readonly RevitConversionContextStack _contextStack;

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
  }

  public Base Build(
    SendSelection sendSelection,
    Action<string, double?>? onOperationProgressed = null,
    CancellationToken ct = default
  )
  {
    List<Element> objects = _contextStack.Current.Document.Document.GetElements(sendSelection.SelectedItems).ToList();

    Base commitObject = new();

    foreach (Element obj in objects)
    {
      ct.ThrowIfCancellationRequested();
      if (_convertedObjectsCache.ContainsBaseConvertedFromId(obj.UniqueId))
      {
        continue;
      }

      commitObject[obj.UniqueId] = _converter.Convert(obj);
    }

    return commitObject;
  }
}
