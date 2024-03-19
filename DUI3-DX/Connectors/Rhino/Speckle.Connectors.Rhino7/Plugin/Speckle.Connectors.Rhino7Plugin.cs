using System;
using System.IO;
using System.Reflection;
using Autofac;
using Rhino.Geometry;
using Rhino.PlugIns;
using Speckle.Autofac.DependencyInjection;
using Speckle.Autofac.DependencyInjection.Extensions;
using Speckle.Autofac.Files;
using Speckle.Connectors.Rhino7.DependencyInjection;
using Speckle.Connectors.Rhino7.HostApp;
using Speckle.Connectors.Rhino7.Interfaces;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models.Extensions;

namespace Speckle.Connectors.Rhino7.Plugin;

///<summary>
/// <para>Every RhinoCommon .rhp assembly must have one and only one PlugIn-derived
/// class. DO NOT create instances of this class yourself. It is the
/// responsibility of Rhino to create an instance of this class.</para>
/// <para>To complete plug-in information, please also see all PlugInDescription
/// attributes in AssemblyInfo.cs (you might need to click "Project" ->
/// "Show All Files" to see it in the "Solution Explorer" window).</para>
///</summary>
public class SpeckleConnectorsRhino7Plugin : PlugIn
{
  private IRhinoPlugin? _rhinoPlugin;

  public AutofacContainer? Container { get; private set; }

  public SpeckleConnectorsRhino7Plugin()
  {
    Instance = this;
  }

  ///<summary>Gets the only instance of the Speckle_Connectors_Rhino7Plugin plug-in.</summary>
  public static SpeckleConnectorsRhino7Plugin Instance { get; private set; }

  // You can override methods here to change the plug-in behavior on
  // loading and shut down, add options pages to the Rhino _Option command
  // and maintain plug-in wide options in a document.

  protected override LoadReturnCode OnLoad(ref string errorMessage)
  {
    try
    {
      AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

      Container = new AutofacContainer(new StorageInfo());
      Container.PreBuildEvent += _container_PreBuildEvent;

      // Register Settings
      var rhinoSettings = new RhinoSettings(HostApplications.Rhino, HostAppVersion.v7);

      Container
        .AddModule(new AutofacRhinoModule())
        .LoadAutofacModules(rhinoSettings.Modules)
        .AddSingletonInstance(rhinoSettings)
        .Build();

      var resolved = Container.Resolve<IRawConversion<Plane, Objects.Geometry.Plane>>();

      // Resolve root plugin object and initialise.
      _rhinoPlugin = Container.Resolve<IRhinoPlugin>();
      _rhinoPlugin.Initialise();

      return LoadReturnCode.Success;
    }
    catch (Exception e) when (!e.IsFatal())
    {
      errorMessage = e.ToFormattedString();
      return LoadReturnCode.ErrorNoDialog;
    }
  }

  protected override void OnShutdown()
  {
    _rhinoPlugin?.Shutdown();
    base.OnShutdown();
  }

  private Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
  {
    // POC: tight binding to files
    Assembly assembly = null;
    string name = args.Name.Split(',')[0];
    string path = Path.GetDirectoryName(typeof(SpeckleConnectorsRhino7Plugin).Assembly.Location);

    if (path != null)
    {
      string assemblyFile = Path.Combine(path, name + ".dll");

      if (File.Exists(assemblyFile))
      {
        assembly = Assembly.LoadFrom(assemblyFile);
      }
    }

    return assembly;
  }

  private void _container_PreBuildEvent(object sender, ContainerBuilder containerBuilder)
  {
    containerBuilder.InjectNamedTypes();
  }
}
