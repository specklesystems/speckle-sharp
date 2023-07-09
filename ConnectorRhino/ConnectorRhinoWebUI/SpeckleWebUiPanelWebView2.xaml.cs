using System;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;

namespace ConnectorRhinoWebUI
{
  /// <summary>
  /// Interaction logic for SpeckleWebUiPanelWebView2.xaml
  /// </summary>
  public partial class SpeckleWebUiPanelWebView2 : UserControl
  {
    public SpeckleWebUiPanelWebView2()
    {
      InitializeComponent();
      Browser.CoreWebView2InitializationCompleted += Browser_Initialized_Completed;
    }

    private void Browser_Initialized_Completed(object sender, EventArgs e)
    {
      Browser.CoreWebView2.OpenDevToolsWindow();
      // NOTE: this is not the correct way of structuring things, for now it's just a hack to get shit going
      var bridge = new WebView2Bridge(new RhinoWebView2UIBinding(Browser), Browser);
      Browser.CoreWebView2.AddHostObjectToScript("WebUIBinding", bridge);
    }
  }
}
