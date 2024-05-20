using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Geometry.ISpeckleObjectToHost;

[NameAndRankValue(nameof(SOG.Mesh), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class MeshToHostConverter : ISpeckleObjectToHostConversion, ITypedConverter<SOG.Mesh, ACG.Multipatch>
{
  private readonly ITypedConverter<List<SOG.Mesh>, ACG.Multipatch> _meshConverter;

  public MeshToHostConverter(ITypedConverter<List<SOG.Mesh>, ACG.Multipatch> meshConverter)
  {
    _meshConverter = meshConverter;
  }

  public object Convert(Base target) => RawConvert((SOG.Mesh)target);

  public ACG.Multipatch RawConvert(SOG.Mesh target)
  {
    return _meshConverter.RawConvert(new List<SOG.Mesh> { target });
  }
}
