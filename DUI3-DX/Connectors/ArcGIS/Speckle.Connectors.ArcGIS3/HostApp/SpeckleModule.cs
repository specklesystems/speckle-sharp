/*

   Copyright 2022 Esri

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       https://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

   See the License for the specific language governing permissions and
   limitations under the License.

*/

using ArcGIS.Desktop.Framework;
using Autofac;
using Speckle.Autofac.DependencyInjection;
using Speckle.Autofac.Files;
using Speckle.Connectors.ArcGIS.DependencyInjetion;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Converters.Common.DependencyInjection;
using Module = ArcGIS.Desktop.Framework.Contracts.Module;

namespace Speckle.Connectors.ArcGIS.HostApp;

/// <summary>
/// This sample shows how to implement pane that contains an Edge WebView2 control using the built-in ArcGIS Pro SDK's WebBrowser control.  For details on how to utilize the WebBrowser control in an add-in see here: https://github.com/Esri/arcgis-pro-sdk/wiki/ProConcepts-Framework#webbrowser  For details on how to utilize the Microsoft Edge web browser control in an add-in see here: https://github.com/Esri/arcgis-pro-sdk/wiki/ProConcepts-Framework#webbrowser-control
/// </summary>
internal class SpeckleModule : Module
{
  private static SpeckleModule s_this;

  /// <summary>
  /// Retrieve the singleton instance to this module here
  /// </summary>
  public static SpeckleModule Current =>
    s_this ??= (SpeckleModule)FrameworkApplication.FindModule("ConnectorArcGIS_Module");

  public static AutofacContainer? Container { get; private set; }

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

  protected override bool Initialize()
  {
    Container = new AutofacContainer(new StorageInfo());
    Container.PreBuildEvent += Container_PreBuildEvent;

    // Register Settings
    var arcgisSettings = new ArcGISSettings(HostApplications.ArcGIS, HostAppVersion.v3);

    Container
      .AddModule(new AutofacArcGISModule())
      .LoadAutofacModules(arcgisSettings.Modules)
      .AddSingletonInstance(arcgisSettings)
      .Build();

    return base.Initialize();
  }

  private void Container_PreBuildEvent(object? sender, ContainerBuilder containerBuilder)
  {
    containerBuilder.InjectNamedTypes<IHostObjectToSpeckleConversion>();
  }
}
