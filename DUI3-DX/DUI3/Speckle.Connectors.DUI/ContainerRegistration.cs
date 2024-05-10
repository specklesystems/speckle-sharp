using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models.Card.SendFilter;
using Speckle.Connectors.DUI.Utils;
using Speckle.Connectors.Utils.Operations;
using Speckle.Core.Transports;
using Speckle.Newtonsoft.Json;
using Speckle.Newtonsoft.Json.Serialization;

namespace Speckle.Connectors.DUI;

public static class ContainerRegistration
{
  public static void AddDUI(this SpeckleContainerBuilder speckleContainerBuilder)
  {
    // send operation and dependencies
    speckleContainerBuilder.AddSingletonInstance<ISyncToMainThread, SyncToUIThread>();
    speckleContainerBuilder.AddTransient<ITransport, ServerTransport>();
    speckleContainerBuilder.AddSingleton<IRootObjectSender, RootObjectSender>();
    
    speckleContainerBuilder.AddSingleton(GetJsonSerializerSettings());
  }
  
  
  

  private static JsonSerializerSettings GetJsonSerializerSettings()
  {
    // Register WebView2 panel stuff
    JsonSerializerSettings settings =
      new()
      {
        Error = (_, args) =>
        {
          // POC: we should probably do a bit more than just swallowing this!
          Console.WriteLine("*** JSON ERROR: " + args.ErrorContext);
        },
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
        Converters = { new DiscriminatedObjectConverter(), new AbstractConverter<DiscriminatedObject, ISendFilter>() }
      };
    return settings;
  }
}
