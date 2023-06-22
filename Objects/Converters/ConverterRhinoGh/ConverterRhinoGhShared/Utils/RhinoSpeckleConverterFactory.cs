using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Objects.Structural.Analysis;
using Objects.Utils;
using Rhino;
using Rhino.DocObjects;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Objects.Converter.RhinoGh.Utils;

public sealed class RhinoSpeckleConverterFactory : ISpeckleConverterFactory
{
  private readonly IServiceProvider serviceProvider;

  public RhinoSpeckleConverterFactory()
  {
    SpeckleLog.Logger.Information($"RhinoSpeckleConverterFactory()");

    var serviceCollection = new ServiceCollection();

    // add services - could  be automated maybe?
    serviceCollection.AddSingleton<IRhinoDictionaryParser, RhinoDictionaryParser>();
    serviceCollection.AddSingleton<IRhinoDocInfo, RhinoDocInfo>();
    serviceCollection.AddSingleton<IRhinoObjectsSchema, RhinoObjectsSchema>();
    serviceCollection.AddSingleton<IRhinoUnits, RhinoUnits>();
    serviceCollection.AddSingleton<IRhinoUserInfo, RhinoUserInfo>();
    serviceCollection.AddTransient<ConverterRhinoGh>();

    serviceProvider = serviceCollection.BuildServiceProvider();
  }

  public ISpeckleConverter Create()
  {
    SpeckleLog.Logger.Information($"Before...");

    var a = serviceProvider.GetService<IRhinoUserInfo>();
    SpeckleLog.Logger.Information($"\ta");

    var b = serviceProvider.GetService<IRhinoDocInfo>();
    SpeckleLog.Logger.Information($"\tb");
    
    var c = serviceProvider.GetService<IRhinoObjectsSchema>();
    SpeckleLog.Logger.Information($"\tc");

    var d = serviceProvider.GetService<IRhinoUnits>();
    SpeckleLog.Logger.Information($"\td");

    var e = serviceProvider.GetService<IRhinoUserInfo>();
    SpeckleLog.Logger.Information($"\te");

    return serviceProvider.GetService<ConverterRhinoGh>();
  }
}
