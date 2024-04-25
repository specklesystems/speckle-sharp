using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Extensions;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.ToSpeckle;
using Speckle.Core.Models;

namespace Speckle.Converters.Revit2023.ToSpeckle;

[NameAndRankValue(nameof(DB.DirectShape), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class DirectShapeConversionToSpeckle : BaseConversionToSpeckle<DB.DirectShape, SOBR.DirectShape>
{
  private readonly RevitConversionContextStack _contextStack;

  private readonly IRawConversion<DB.Mesh, SOG.Mesh> _meshConverter;
  private readonly IRawConversion<DB.Solid, List<SOG.Mesh>> _solidConverter;
  private readonly ParameterObjectAssigner _parameterObjectAssigner;

  public DirectShapeConversionToSpeckle(
    IRawConversion<DB.Mesh, SOG.Mesh> meshConverter,
    ParameterObjectAssigner parameterObjectAssigner,
    IRawConversion<DB.Solid, List<SOG.Mesh>> solidConverter,
    RevitConversionContextStack contextStack
  )
  {
    _meshConverter = meshConverter;
    _parameterObjectAssigner = parameterObjectAssigner;
    _solidConverter = solidConverter;
    _contextStack = contextStack;
  }

  public override SOBR.DirectShape RawConvert(DB.DirectShape target)
  {
    var category = target.Category.GetBuiltInCategory().GetSchemaBuilderCategoryFromBuiltIn();
    var element = target.get_Geometry(new DB.Options());

    var geometries = new List<Base>(); // POC: This could be an list of meshes.
    foreach (var geometryObject in element)
    {
      switch (geometryObject)
      {
        case DB.Mesh mesh:
          geometries.Add(_meshConverter.RawConvert(mesh));
          break;
        case DB.Solid solid:
          geometries.AddRange(_solidConverter.RawConvert(solid));
          break;
      }
    }

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
