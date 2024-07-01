using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.ToHost.TopLevel;

[NameAndRankValue(nameof(SOG.Mesh), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class MeshToHostConverter : IToHostTopLevelConverter, ITypedConverter<SOG.Mesh, ACG.Multipatch>
{
  private readonly ITypedConverter<List<SOG.Mesh>, ACG.Multipatch> _meshConverter;

  public MeshToHostConverter(ITypedConverter<List<SOG.Mesh>, ACG.Multipatch> meshConverter)
  {
    _meshConverter = meshConverter;
  }

  public object Convert(Base target) => Convert((SOG.Mesh)target);

  public ACG.Multipatch Convert(SOG.Mesh target)
  {
    return _meshConverter.Convert(new List<SOG.Mesh> { target });
  }
}
