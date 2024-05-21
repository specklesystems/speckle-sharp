using Rhino.PlugIns;
using Speckle.Autofac;
using Speckle.Autofac.DependencyInjection;
using Speckle.Autofac.Files;
using Speckle.Connectors.Rhino7.DependencyInjection;
using Speckle.Connectors.Rhino7.HostApp;
using Speckle.Connectors.Rhino7.Interfaces;
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

  protected override string LocalPlugInName => "Speckle (New UI)";
  public AutofacContainer? Container { get; private set; }

  public SpeckleConnectorsRhino7Plugin()
  {
    Instance = this;
  }

  ///<summary>Gets the only instance of the Speckle_Connectors_Rhino7Plugin plug-in.</summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
  public static SpeckleConnectorsRhino7Plugin Instance { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

  // You can override methods here to change the plug-in behavior on
  // loading and shut down, add options pages to the Rhino _Option command
  // and maintain plug-in wide options in a document.

  protected override LoadReturnCode OnLoad(ref string errorMessage)
  {
    try
    {
      AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolver.OnAssemblyResolve<SpeckleConnectorsRhino7Plugin>;

      Container = new AutofacContainer(new StorageInfo());

      // Register Settings
      var rhinoSettings = new RhinoSettings(HostApplications.Rhino, HostAppVersion.v7);

      // POC: We must load the Rhino connector module manually because we only search for DLL files when calling `LoadAutofacModules`,
      // but the Rhino connector has `.rhp` as it's extension.
      Container
        .AddModule(new AutofacRhinoModule())
        .LoadAutofacModules(rhinoSettings.Modules)
        .AddSingletonInstance(rhinoSettings)
        .Build();

      // Resolve root plugin object and initialise.
      _rhinoPlugin = Container.Resolve<IRhinoPlugin>();
      _rhinoPlugin.Initialise();

      return LoadReturnCode.Success;
    }
    catch (Exception e) when (!e.IsFatal())
    {
      errorMessage = e.ToFormattedString();
      return LoadReturnCode.ErrorShowDialog;
    }
  }

  protected override void OnShutdown()
  {
    _rhinoPlugin?.Shutdown();
    base.OnShutdown();
  }
}
