using Microsoft.Extensions.Logging;
using Serilog;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Common.DependencyInjection;

public static class ContainerRegistration
{
  public static void AddConverterCommon(this SpeckleContainerBuilder speckleContainerBuilder)
  {
    speckleContainerBuilder.RegisterRawConversions();
    speckleContainerBuilder.InjectNamedTypes<IHostObjectToSpeckleConversion>();
    speckleContainerBuilder.InjectNamedTypes<ISpeckleObjectToHostConversion>();

    // POC: will likely need refactoring with our reporting pattern.
    var serilogLogger = new LoggerConfiguration().MinimumLevel
      .Debug()
      .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
      .CreateLogger();

    ILoggerFactory loggerFactory = new LoggerFactory().AddSerilog(serilogLogger);
    speckleContainerBuilder.AddSingletonInstance(loggerFactory);
  }
}
