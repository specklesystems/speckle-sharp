﻿using Speckle.Converters.Common;
using Speckle.Converters.RevitShared.Extensions;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.ToSpeckle;
using Speckle.Core.Models;

namespace Speckle.Converters.Revit2023.ToSpeckle;

[NameAndRankValue(nameof(DB.DirectShape), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class DirectShapeTopLevelConverterToSpeckle : BaseTopLevelConverterToSpeckle<DB.DirectShape, SOBR.DirectShape>
{
  private readonly IRevitConversionContextStack _contextStack;
  private readonly ParameterObjectAssigner _parameterObjectAssigner;
  private readonly DisplayValueExtractor _displayValueExtractor;

  public DirectShapeTopLevelConverterToSpeckle(
    ParameterObjectAssigner parameterObjectAssigner,
    IRevitConversionContextStack contextStack,
    DisplayValueExtractor displayValueExtractor
  )
  {
    _parameterObjectAssigner = parameterObjectAssigner;
    _contextStack = contextStack;
    _displayValueExtractor = displayValueExtractor;
  }

  public override SOBR.DirectShape Convert(DB.DirectShape target)
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
