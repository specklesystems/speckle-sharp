using Eto.Drawing;
using Eto.Forms;
using Rhino.UI;

#if RHINO7
namespace SpeckleRhino
{
  /// <summary>
  /// Required class GUID, used as the panel Id
  /// </summary>
  [System.Runtime.InteropServices.Guid("EA93829C-67EA-449E-B69D-8FCC39D4B1E0")]
  public class WebUIPanel : Panel, IPanel
  {
    readonly uint m_document_sn = 0;
    WebView webView;

    /// <summary>
    /// Provide easy access to the SampleCsEtoPanel.GUID
    /// </summary>
    public static System.Guid PanelId => typeof(WebUIPanel).GUID;

    /// <summary>
    /// Required public constructor with NO parameters
    /// </summary>
    public WebUIPanel(uint documentSerialNumber)
    {
      m_document_sn = documentSerialNumber;

      Title = GetType().Name;

      Eto.Wpf.Forms.Controls.WebView2Loader.InstallMode = Eto.Wpf.Forms.Controls.WebView2InstallMode.Manual;

      Eto.Wpf.Forms.Controls.WebView2Handler.GetCoreWebView2Environment = () =>
      {
        var userDataFolder = Rhino.RhinoApp.GetDataDirectory(true, true);
        return Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
      };

      webView = new WebView();
      webView.Width = 640;
      webView.Height = 480;

      webView.DocumentLoading += (sender, e) =>
      {
        Microsoft.Web.WebView2.Wpf.WebView2 webView2 = (Microsoft.Web.WebView2.Wpf.WebView2)webView.ControlObject;
        if (webView2 != null)
        {
          Microsoft.Web.WebView2.Core.CoreWebView2 coreWebView2 = webView2.CoreWebView2;
          if (coreWebView2 != null)
          {
            var bindings = new Speckle.ConnectorRhino.UI.RhinoWebUIBindings();
            bindings.CoreWebView2 = coreWebView2;
            coreWebView2.AddHostObjectToScript("UiBindings", bindings);

          }

        }

        // old method
        if (e.Uri.Scheme == "myscheme")
        {
          e.Cancel = true; // prevent navigation

          var path = e.Uri.PathAndQuery;
          if (path == "dosomething")
          {
            // do something..
          }
        }
      };

#if DEBUG
      webView.Url = new System.Uri("http://localhost:8080/#/");
#else
      webView.Url = new System.Uri("https://dashing-haupia-e8f6e3.netlify.app/");
#endif


      // webView.Url = new System.Uri("");

      var layout = new DynamicLayout { DefaultSpacing = new Size(5, 5), Padding = new Padding(10) };
      layout.AddSeparateRow(webView, null);
      layout.Add(null);
      Content = layout;
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing && (webView != null))
      {
        webView.Dispose();
      }
      base.Dispose(disposing);
    }

    public string Title { get; }

    #region IPanel methods
    public void PanelShown(uint documentSerialNumber, ShowPanelReason reason)
    {
      // Called when the panel tab is made visible, in Mac Rhino this will happen
      // for a document panel when a new document becomes active, the previous
      // documents panel will get hidden and the new current panel will get shown.
      Rhino.RhinoApp.WriteLine($"Panel shown for document {documentSerialNumber}, this serial number {m_document_sn} should be the same");
    }

    public void PanelHidden(uint documentSerialNumber, ShowPanelReason reason)
    {
      // Called when the panel tab is hidden, in Mac Rhino this will happen
      // for a document panel when a new document becomes active, the previous
      // documents panel will get hidden and the new current panel will get shown.
      Rhino.RhinoApp.WriteLine($"Panel hidden for document {documentSerialNumber}, this serial number {m_document_sn} should be the same");
    }

    public void PanelClosing(uint documentSerialNumber, bool onCloseDocument)
    {
      // Called when the document or panel container is closed/destroyed
      Rhino.RhinoApp.WriteLine($"Panel closing for document {documentSerialNumber}, this serial number {m_document_sn} should be the same");
    }
    #endregion IPanel methods
  }
}
#endif
