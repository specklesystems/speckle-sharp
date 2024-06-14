using Microsoft.Extensions.Logging;
using Serilog;
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.Utils.Cancellation;
using Speckle.Connectors.Utils.Operations;
using Speckle.Core.Logging;

namespace Speckle.Connectors.Utils;

public static class ContainerRegistration
{
  public static void AddConnectorUtils(this SpeckleContainerBuilder builder)
  {
    // send operation and dependencies
    builder.AddSingleton<CancellationManager>();
    builder.AddScoped<ReceiveOperation>();

    // POC: will likely need refactoring with our reporting pattern.
    //TODO: Logger will likly be removed from Core, we'll plan to figure out the config later...
    var serilogLogger = SpeckleLog.Logger;

    ILoggerFactory loggerFactory = new LoggerFactory().AddSerilog(serilogLogger);
    builder.AddSingleton(loggerFactory);
  }
}
