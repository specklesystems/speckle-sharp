using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using CefSharp;
using CefSharp.JavascriptBinding;

namespace ConnectorRhinoWebUI
{
  /// <summary>
  /// Interaction logic for SpeckleWebUIPanel.xaml
  /// </summary>
  public partial class SpeckleWebUIPanel : UserControl
  {
    public SpeckleWebUIPanel()
    {
      InitializeComponent();
      Browser.IsBrowserInitializedChanged += Browser_IsBrowserInitializedChanged;
      Browser.JavascriptObjectRepository.ResolveObject += JavascriptObjectRepository_ResolveObject;
    }

    private void JavascriptObjectRepository_ResolveObject(object sender, CefSharp.Event.JavascriptBindingEventArgs e)
    {
      var repo = e.ObjectRepository;
      if (e.ObjectName == "WebUIBinding")
      {
        try
        {
          repo.NameConverter = new CamelCaseJavascriptNameConverter();
          repo.Register("WebUIBinding", new WebUIBinding(), true, BindingOptions.DefaultBinder);
        }
        catch (Exception ex)
        {
          Debug.Write(ex);
        }
      }
    }

    private void Browser_IsBrowserInitializedChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      Browser.ShowDevTools();
    }
  }
}
