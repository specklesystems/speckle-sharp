using Speckle.Converters.Common;
using Speckle.Converters.RevitShared.Extensions;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Core.Models;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.RevitShared.ToSpeckle;

[NameAndRankValue(nameof(IRevitDirectShape), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class DirectShapeTopLevelConverterToSpeckle : BaseTopLevelConverterToSpeckle<IRevitDirectShape, SOBR.DirectShape>
{
  private readonly IConversionContextStack<IRevitDocument, IRevitForgeTypeId> _contextStack;
  private readonly IParameterObjectAssigner _parameterObjectAssigner;
  private readonly IDisplayValueExtractor _displayValueExtractor;

  public DirectShapeTopLevelConverterToSpeckle(
    IParameterObjectAssigner parameterObjectAssigner,
    IConversionContextStack<IRevitDocument, IRevitForgeTypeId> contextStack,
    IDisplayValueExtractor displayValueExtractor
  )
  {
    _parameterObjectAssigner = parameterObjectAssigner;
    _contextStack = contextStack;
    _displayValueExtractor = displayValueExtractor;
  }

  public override SOBR.DirectShape Convert(IRevitDirectShape target)
  {
    var category = target.Category.GetBuiltInCategory().GetSchemaBuilderCategoryFromBuiltIn();

    // POC: Making the analogy that the DisplayValue is the same as the Geometries is only valid while we don't support Solids on send.
    var geometries = _displayValueExtractor.GetDisplayValue(target).Cast<Base>().ToList();

    SOBR.DirectShape result =
      new(target.Name, category, geometries)
      {
        displayValue = geometries,
        units = _contextStack.Current.SpeckleUnits,
        elementId = target.Id.ToString()
      };

    _parameterObjectAssigner.AssignParametersToBase(target, result);

    result["type"] = target.Name;

    return result;
  }
}
