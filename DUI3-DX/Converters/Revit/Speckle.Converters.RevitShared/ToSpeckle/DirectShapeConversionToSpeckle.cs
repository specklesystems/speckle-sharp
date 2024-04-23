using Objects.Other;
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
  private readonly IRawConversion<DB.Material, RenderMaterial> _materialConverter;
  private readonly ParameterObjectAssigner _parameterObjectAssigner;

  public DirectShapeConversionToSpeckle(
    RevitConversionContextStack contextStack,
    IRawConversion<DB.Mesh, SOG.Mesh> meshConverter,
    IRawConversion<DB.Material, RenderMaterial> materialConverter,
    ParameterObjectAssigner parameterObjectAssigner
  )
  {
    _contextStack = contextStack;
    _meshConverter = meshConverter;
    _materialConverter = materialConverter;
    _parameterObjectAssigner = parameterObjectAssigner;
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
          var doc = _contextStack.Current.Document.Document;
          foreach (DB.Face face in solid.Faces)
          {
            DB.Mesh revitMesh = face.Triangulate();
            SOG.Mesh speckleMesh = _meshConverter.RawConvert(revitMesh);

            // Override mesh material (which in this case should be null) with face material if it exists.
            if (doc.GetElement(face.MaterialElementId) is DB.Material faceMaterial)
            {
              var speckleMaterial = _materialConverter.RawConvert(faceMaterial);
              speckleMesh["renderMaterial"] = speckleMaterial; // POC: Mesh not having a typed renderMaterial is madness
            }

            geometries.Add(speckleMesh);
          }
          break;
      }
    }

    SOBR.DirectShape result = new(target.Name, category, geometries) { displayValue = geometries };

    // POC: Parameter extractor exlodes with DS, returns a null elementType somewhere.
    _parameterObjectAssigner.AssignParametersToBase(target, result);

    result["type"] = target.Name;

    return result;
  }
}
