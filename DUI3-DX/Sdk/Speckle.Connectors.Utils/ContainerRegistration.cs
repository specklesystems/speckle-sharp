using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.Utils.Cancellation;
using Speckle.Connectors.Utils.Operations;

namespace Speckle.Connectors.Utils;

public static class ContainerRegistration
{
  public static void AddConnectorUtils(this SpeckleContainerBuilder builder)
  {
    // send operation and dependencies
    builder.AddSingleton<CancellationManager>();
    builder.AddScoped<ReceiveOperation>();

    // POC: will likely need refactoring with our reporting pattern.
    var serilogLogger = new LoggerConfiguration().MinimumLevel
      .Debug()
      .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
      .CreateLogger();

    var serviceCollection = new ServiceCollection();
    serviceCollection.AddLogging(x => x.AddSerilog(serilogLogger));
    builder.Populate(serviceCollection);
  }
}
