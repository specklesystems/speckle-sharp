using ArcGIS.Desktop.Framework;
using Autofac;
using Speckle.Autofac;
using Speckle.Autofac.DependencyInjection;
using Speckle.Autofac.Files;
using Speckle.Connectors.ArcGIS.HostApp;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Converters.Common.DependencyInjection;
using Module = ArcGIS.Desktop.Framework.Contracts.Module;

namespace Speckle.Connectors.ArcGIS;

/// <summary>
/// This sample shows how to implement pane that contains an Edge WebView2 control using the built-in ArcGIS Pro SDK's WebBrowser control.  For details on how to utilize the WebBrowser control in an add-in see here: https://github.com/Esri/arcgis-pro-sdk/wiki/ProConcepts-Framework#webbrowser  For details on how to utilize the Microsoft Edge web browser control in an add-in see here: https://github.com/Esri/arcgis-pro-sdk/wiki/ProConcepts-Framework#webbrowser-control
/// </summary>
internal sealed class SpeckleModule : Module
{
  private static SpeckleModule? s_this;

  /// <summary>
  /// Retrieve the singleton instance to this module here
  /// </summary>
  public static SpeckleModule Current =>
    s_this ??= (SpeckleModule)FrameworkApplication.FindModule("ConnectorArcGIS_Module");

  public AutofacContainer Container { get; }

  public SpeckleModule()
  {
    AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolver.OnAssemblyResolve<SpeckleModule>;

    Container = new AutofacContainer(new StorageInfo());
    Container.PreBuildEvent += Container_PreBuildEvent;

    // Register Settings
    var arcgisSettings = new ArcGISSettings(HostApplications.ArcGIS, HostAppVersion.v3);

    Container.LoadAutofacModules(arcgisSettings.Modules).AddSingletonInstance(arcgisSettings).Build();
  }

  /// <summary>
  /// Called by Framework when ArcGIS Pro is closing
  /// </summary>
  /// <returns>False to prevent Pro from closing, otherwise True</returns>
  protected override bool CanUnload()
  {
    //TODO - add your business logic
    //return false to ~cancel~ Application close
    return true;
  }

  private static void Container_PreBuildEvent(object? sender, ContainerBuilder containerBuilder)
  {
    containerBuilder.RegisterRawConversions();
    containerBuilder.InjectNamedTypes<IHostObjectToSpeckleConversion>();
    containerBuilder.InjectNamedTypes<ISpeckleObjectToHostConversion>();
  }
}
