using Speckle.Converters.Common;
using Speckle.Core.Models;
using Speckle.InterfaceGenerator;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.Revit2023.ToSpeckle;


// POC: do we need to see the blocks investigation outcome? Does the current logic hold?
// opportunity to rethink or confirm hosted element handling? Should this be a connector responsibiliy?
// No interfacing out however...
// CNX-9414 Re-evaluate hosted element conversions
[GenerateAutoInterface]
public class HostedElementConversionToSpeckle : IHostedElementConversionToSpeckle
{
  private readonly IRootToSpeckleConverter _converter;
  private readonly IConversionContextStack<IRevitDocument, IRevitForgeTypeId> _contextStack;

  public HostedElementConversionToSpeckle(
    IRootToSpeckleConverter converter,
    IConversionContextStack<IRevitDocument, IRevitForgeTypeId> contextStack
  )
  {
    _converter = converter;
    _contextStack = contextStack;
  }

  public IEnumerable<Base> ConvertHostedElements(IEnumerable<IRevitElementId> hostedElementIds)
  {
    foreach (var elemId in hostedElementIds)
    {
      IRevitElement element = _contextStack.Current.Document.GetElement(elemId);

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
