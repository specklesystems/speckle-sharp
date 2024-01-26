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

using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Threading;
using DUI3;
using Microsoft.Web.WebView2.Core;
using Speckle.Core.Logging;

namespace ConnectorArcGIS;

/// <summary>
/// Interaction logic for WebViewBrowserView.xaml
/// </summary>
public partial class SpeckleDUI3 : UserControl
{
  public SpeckleDUI3()
  {
    InitializeComponent();
    Browser.CoreWebView2InitializationCompleted += OnInitialized;
  }

  private void ShowDevToolsMethod() => Browser.CoreWebView2.OpenDevToolsWindow();

  private void ExecuteScriptAsyncMethod(string script)
  {
    if (!Browser.IsInitialized)
    {
      throw new SpeckleException("Failed to execute script, Webview2 is not initialized yet.");
    }

    Browser.Dispatcher.Invoke(() => Browser.ExecuteScriptAsync(script), DispatcherPriority.Background);
  }

  private void OnInitialized(object sender, CoreWebView2InitializationCompletedEventArgs e)
  {
    List<IBinding> bindings = Bindings.Factory.CreateBindings();

    foreach (IBinding binding in bindings)
    {
      BrowserBridge bridge = new(Browser, binding, ExecuteScriptAsyncMethod, ShowDevToolsMethod);
      Browser.CoreWebView2.AddHostObjectToScript(binding.Name, bridge);
    }
  }
}
