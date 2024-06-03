using Autodesk.Revit.DB;
using Speckle.Converters.Common;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Core.Models;
using Speckle.Revit2023.Api;

namespace Speckle.Converters.RevitShared.ToSpeckle;

// POC: do we need to see the blocks investigation outcome? Does the current logic hold?
// opportunity to rethink or confirm hosted element handling? Should this be a connector responsibiliy?
// No interfacing out however...
// CNX-9414 Re-evaluate hosted element conversions
public class HostedElementConversionToSpeckle
{
  private readonly ToSpeckleConvertedObjectsCache _convertedObjectsCache;
  private readonly IRootToSpeckleConverter _converter;
  private readonly IRevitConversionContextStack _contextStack;

  public HostedElementConversionToSpeckle(
    ToSpeckleConvertedObjectsCache convertedObjectsCache,
    IRootToSpeckleConverter converter,
    IRevitConversionContextStack contextStack
  )
  {
    _convertedObjectsCache = convertedObjectsCache;
    _converter = converter;
    _contextStack = contextStack;
  }

  public IEnumerable<Base> ConvertHostedElements(IEnumerable<ElementId> hostedElementIds)
  {
    foreach (var elemId in hostedElementIds)
    {
      var element = _contextStack.Current.Document.GetElement(elemId);

      Base @base;
      try
      {
        @base = _converter.Convert(element);
      }
      catch (SpeckleConversionException)
      {
        // POC: logging
        continue;
      }

      yield return @base;
    }
  }
}
