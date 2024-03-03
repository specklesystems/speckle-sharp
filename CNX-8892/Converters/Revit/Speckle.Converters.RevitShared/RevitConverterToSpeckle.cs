using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.RevitShared;

// POC: maybe possible to restrict the access so this cannot be created directly?
public class RevitConverterToSpeckle : ISpeckleConverterToSpeckle
{
  private readonly IFactory<string, IHostObjectToSpeckleConversion> _toSpeckle;

  public RevitConverterToSpeckle(IFactory<string, IHostObjectToSpeckleConversion> toSpeckle)
  {
    _toSpeckle = toSpeckle;
  }

  public void Convert() => throw new System.NotImplementedException();
}
