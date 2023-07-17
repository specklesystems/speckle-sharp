using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using CefSharp.Wpf;
using CefSharp;
using DUI3;
using Sentry.Protocol;
using Speckle.ConnectorRevitDUI3.Bindings;

namespace Speckle.ConnectorRevitDUI3;

public class App : IExternalApplication
{
  public static UIApplication AppInstance { get; set; }

  public static UIControlledApplication UICtrlApp { get; set; }

  public Result OnStartup(UIControlledApplication application)
  {
    
    UICtrlApp = application;
    UICtrlApp.ControlledApplication.ApplicationInitialized += ControlledApplicationOnApplicationInitialized;

    string tabName = "Speckle";
    try
    {
      application.CreateRibbonTab(tabName);
    }
    catch (Exception e)
    {
      Debug.WriteLine(e.Message);
    }

    var specklePanel = application.CreateRibbonPanel(tabName, "Speckle 2 DUI3");
    var speckleButton = specklePanel.AddItem(new PushButtonData("Speckle 2 DUI3", "Revit Connector", typeof(App).Assembly.Location, typeof(SpeckleRevitDUI3Command).FullName)) as PushButton;
    return Result.Succeeded;
  }

  private void ControlledApplicationOnApplicationInitialized(object sender, ApplicationInitializedEventArgs e)
  {
    AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(OnAssemblyResolve);
    AppInstance = new UIApplication(sender as Application);
    
    RegisterDockablePane(UICtrlApp);
  }

  internal static DockablePaneId PanelId = new DockablePaneId(new Guid("{85F73DA4-3EF4-4870-BDBC-FD2D238EED31}"));
  public static Panel Panel { get; set; }
  public void RegisterDockablePane(UIControlledApplication application)
  {
    CefSharpSettings.ConcurrentTaskExecution = true;
    
    Panel = new Panel();
    application.RegisterDockablePane(PanelId, "Speckle DUI3", Panel);
    
    var browser = Panel.Browser; 
    
    // browser.JavascriptObjectRepository.NameConverter = null;
    browser.IsBrowserInitializedChanged += (sender, e) =>
    {
      var executeScriptAsyncMethod = (string script) => {
        Debug.WriteLine(script);
        browser.EvaluateScriptAsync(script);
      };
      var showDevToolsMethod = () => browser.ShowDevTools();

      var testBinding = new TestBinding();
      var testBindingBridge = new BrowserBridge(browser, testBinding, executeScriptAsyncMethod, showDevToolsMethod);
      browser.JavascriptObjectRepository.Register(testBindingBridge.FrontendBoundName, testBindingBridge, true);
      
      var baseBinding = new RevitBaseBinding();
      var baseBindingBridge = new BrowserBridge(browser, baseBinding, executeScriptAsyncMethod, showDevToolsMethod);
      browser.JavascriptObjectRepository.Register(baseBindingBridge.FrontendBoundName,baseBindingBridge, true );
    };

  }

  public Result OnShutdown(UIControlledApplication application)
  {
    return Result.Succeeded;
  }
  
  static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
  {
    Assembly a = null;
    var name = args.Name.Split(',')[0];
    string path = Path.GetDirectoryName(typeof(App).Assembly.Location);

    string assemblyFile = Path.Combine(path, name + ".dll");

    if (File.Exists(assemblyFile))
      a = Assembly.LoadFrom(assemblyFile);

    return a;
  }

}

