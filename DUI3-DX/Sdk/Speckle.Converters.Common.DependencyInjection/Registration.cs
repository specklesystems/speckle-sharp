using Speckle.Autofac.DependencyInjection.Registration;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Common.DependencyInjection;

public static class ContainerRegistration
{
  public static void AddConverterCommon(this SpeckleContainerBuilder speckleContainerBuilder)
  {
    speckleContainerBuilder.RegisterRawConversions();
    speckleContainerBuilder.InjectNamedTypes<IHostObjectToSpeckleConversion>();
    speckleContainerBuilder.InjectNamedTypes<ISpeckleObjectToHostConversion>();
  }
}
