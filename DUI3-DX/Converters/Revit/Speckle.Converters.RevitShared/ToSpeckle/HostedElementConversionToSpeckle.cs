using Autodesk.Revit.DB;
using Speckle.Converters.Common;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;

namespace Speckle.Converters.RevitShared.ToSpeckle;

// POC: do we need to see the blocks investigation outcome? Does the current logic hold?
// opportunity to rethink or confirm hosted element handling? Should this be a connector responsibiliy?
// No interfacing out however...
public class HostedElementConversionToSpeckle
{
  private readonly ToSpeckleConvertedObjectsCache _convertedObjectsCache;
  private readonly ISpeckleConverterToSpeckle _converter;

  public HostedElementConversionToSpeckle(
    ToSpeckleConvertedObjectsCache convertedObjectsCache,
    ISpeckleConverterToSpeckle converter
  )
  {
    _convertedObjectsCache = convertedObjectsCache;
    _converter = converter;
  }

  public void AssignHostedElements(DB.Element element, Base @base)
  {
    List<Base> hostedObjects = GetHostedElementsConverted(element);
    if (hostedObjects.Count > 0)
    {
      if (@base.GetDetachedProp("elements") is List<Base> elements)
      {
        elements.AddRange(hostedObjects);
      }
      else
      {
        @base.SetDetachedProp("elements", hostedObjects);
      }
    }
  }

  public List<Base> GetHostedElementsConverted(Element host)
  {
    var convertedHostedElements = new List<Base>();

    var hostedElementIds = GetHostedElementIds(host);

    foreach (var elemId in hostedElementIds)
    {
      var element = host.Document.GetElement(elemId);
      if (_convertedObjectsCache.ContainsBaseConvertedFromId(element.UniqueId))
      {
        continue;
      }

      convertedHostedElements.Add(_converter.Convert(element));
    }

    return convertedHostedElements;
  }

  private static IList<ElementId> GetHostedElementIds(Element host)
  {
    IList<ElementId> ids;
    if (host is HostObject hostObject)
    {
      ids = hostObject.FindInserts(true, false, false, false);
    }
    else
    {
      var typeFilter = new ElementIsElementTypeFilter(true);
      var categoryFilter = new ElementMulticategoryFilter(
        new List<BuiltInCategory>()
        {
          BuiltInCategory.OST_CLines,
          BuiltInCategory.OST_SketchLines,
          BuiltInCategory.OST_WeakDims
        },
        true
      );
      ids = host.GetDependentElements(new LogicalAndFilter(typeFilter, categoryFilter));
    }

    // dont include host elementId
    ids.Remove(host.Id);

    return ids;
  }
}
