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
using ArcGIS.Desktop.Framework.Contracts;

namespace Speckle.Connectors.ArcGIS;

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

  #region Overrides
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

  #endregion Overrides
}
