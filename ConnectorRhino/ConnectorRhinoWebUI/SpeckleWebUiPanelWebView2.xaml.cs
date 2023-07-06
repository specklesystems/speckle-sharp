using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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

    private void Browser_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
    {
      throw new NotImplementedException();
    }

    private void Browser_Initialized_Completed(object sender, EventArgs e)
    {
      Browser.CoreWebView2.OpenDevToolsWindow();
      Browser.CoreWebView2.AddHostObjectToScript("WebUIBinding", new RhinoWebView2UIBinding(Browser));
    }
  }
}
