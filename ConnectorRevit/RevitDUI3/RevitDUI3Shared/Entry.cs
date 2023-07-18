using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using CefSharp;
using DUI3;
using Speckle.ConnectorRevitDUI3.Bindings;

namespace Speckle.ConnectorRevitDUI3;

public class App : IExternalApplication
{
  private static UIApplication AppInstance { get; set; }

  private static UIControlledApplication UICtrlApp { get; set; }

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
    
    RegisterDockablePane(AppInstance);
  }

  internal static readonly DockablePaneId PanelId = new DockablePaneId(new Guid("{85F73DA4-3EF4-4870-BDBC-FD2D238EED31}"));
  public static Panel Panel { get; private set; }
  private void RegisterDockablePane(UIApplication application)
  {
    CefSharpSettings.ConcurrentTaskExecution = true;
    
    Panel = new Panel();
    UICtrlApp.RegisterDockablePane(PanelId, "Speckle DUI3", Panel);
    
    var browser = Panel.Browser; 
    
    browser.IsBrowserInitializedChanged += (sender, e) =>
    {
      var executeScriptAsyncMethod = (string script) => {
        Debug.WriteLine(script);
        browser.EvaluateScriptAsync(script);
      };
      var showDevToolsMethod = () => browser.ShowDevTools();

      // browser.JavascriptObjectRepository.NameConverter = null; // not available in cef65, we need the below
      var bindingOptions = new BindingOptions() { CamelCaseJavascriptNames = false };
      
      var testBinding = new TestBinding();
      var testBindingBridge = new BrowserBridge(browser, testBinding, executeScriptAsyncMethod, showDevToolsMethod);
      browser.JavascriptObjectRepository.Register(testBindingBridge.FrontendBoundName, testBindingBridge, true, bindingOptions);
      
      var baseBinding = new RevitBaseBinding(application);
      var baseBindingBridge = new BrowserBridge(browser, baseBinding, executeScriptAsyncMethod, showDevToolsMethod);
      browser.JavascriptObjectRepository.Register(baseBindingBridge.FrontendBoundName,baseBindingBridge, true, bindingOptions);

#if  REVIT2020
      // NOTE: Cef65 does not work with DUI3 in yarn dev. To test things you need to do `yarn build` and serve the build
      // folder at port 3000 (or change it to something else if you want to).
      // Guru  meditation: Je sais, pas ideal. Mais q'est que nous pouvons faire? Rien. C'est l'autodesk vie.
      browser.Load("http://localhost:3000");
#endif
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

    if (path != null)
    {
      string assemblyFile = Path.Combine(path, name + ".dll");

      if (File.Exists(assemblyFile))
        a = Assembly.LoadFrom(assemblyFile);
    }

    return a;
  }

}

