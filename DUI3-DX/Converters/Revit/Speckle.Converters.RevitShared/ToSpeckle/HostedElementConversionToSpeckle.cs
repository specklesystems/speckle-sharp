using Autodesk.Revit.DB;
using Speckle.Converters.Common;
using Speckle.Converters.RevitShared.Extensions;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Core.Models;

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

  public List<Base> GetHostedElementsConverted(Element host)
  {
    return GetHostedElementsConvertedFromIds(host, host.GetHostedElementIds());
  }

  public List<Base> GetHostedElementsConvertedFromIds(Element host, IList<ElementId> hostedElementIds)
  {
    var convertedHostedElements = new List<Base>();
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
}
