using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using CefSharp;
using DUI3;
using DUI3.Bindings;
using Sentry.Protocol;
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
    AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
    AppInstance = new UIApplication(sender as Application);
    
    RegisterDockablePane(AppInstance);
  }

  internal static readonly DockablePaneId PanelId = new(new Guid("{85F73DA4-3EF4-4870-BDBC-FD2D238EED31}"));
  
  public static Panel Panel { get; private set; }
  
  private void RegisterDockablePane(UIApplication application)
  {
    CefSharpSettings.ConcurrentTaskExecution = true;
    
    Panel = new Panel();
    UICtrlApp.RegisterDockablePane(PanelId, "Speckle DUI3", Panel);
    
    var browser = Panel.Browser; 
    
#if REVIT2020
      // browser.JavascriptObjectRepository.NameConverter = null; // not available in cef65, we need the below
      var bindingOptions = new BindingOptions() { CamelCaseJavascriptNames = false };
#endif
#if REVIT2023
    browser.JavascriptObjectRepository.NameConverter = null;
    BindingOptions bindingOptions = null;
#endif
    
    // TODO: these methods should probably be moved to browser specific helper projects
    void ExecuteScriptAsyncMethod(string script)
    {
      Debug.WriteLine(script);
      if(browser.CanExecuteJavascriptInMainFrame) {
        browser.EvaluateScriptAsync(script);
      }
      else
      {
        // TODO: Log 
      }
    }

    void ShowDevToolsMethod() => browser.ShowDevTools();
    
    var testBinding = new TestBinding();
    var testBindingBridge = new BrowserBridge(browser, testBinding, ExecuteScriptAsyncMethod, ShowDevToolsMethod);
    
    var baseBinding = new BasicConnectorBindingRevit(application);
    var baseBindingBridge = new BrowserBridge(browser, baseBinding, ExecuteScriptAsyncMethod, ShowDevToolsMethod);

    var configBinding = new ConfigBinding();
    var configBindingBridge = new BrowserBridge(browser, configBinding, ExecuteScriptAsyncMethod, ShowDevToolsMethod);
    
    browser.IsBrowserInitializedChanged += (sender, e) =>
    {
      browser.JavascriptObjectRepository.Register(testBindingBridge.FrontendBoundName, testBindingBridge, true, bindingOptions);
      browser.JavascriptObjectRepository.Register(baseBindingBridge.FrontendBoundName, baseBindingBridge, true, bindingOptions);
      browser.JavascriptObjectRepository.Register(configBindingBridge.FrontendBoundName, configBindingBridge, true, bindingOptions);
      
#if  REVIT2020
      // NOTE: Cef65 does not work with DUI3 in yarn dev. To test things you need to do `yarn build` and serve the build
      // folder at port 3000 (or change it to something else if you want to).
      // Guru  meditation: Je sais, pas ideal. Mais q'est que nous pouvons faire? Rien. C'est l'autodesk vie.
      // NOTE: To run the ui from a build, follow these steps: 
      // - run `yarn build` in the DUI3 folder
      // - run ` PORT=3003  node .output/server/index.mjs` after the build
      browser.Load("http://localhost:3003");
      ShowDevToolsMethod();
#endif
#if REVIT2023
      browser.Load("http://localhost:8082");
#endif
    };

  }

  public Result OnShutdown(UIControlledApplication application)
  {
    return Result.Succeeded;
  }
  
  static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
  {
    Assembly assembly = null;
    var name = args.Name.Split(',')[0];
    string path = Path.GetDirectoryName(typeof(App).Assembly.Location);

    if(path != null)
    {
      string assemblyFile = Path.Combine(path, name + ".dll");

      if (File.Exists(assemblyFile))
        assembly = Assembly.LoadFrom(assemblyFile);
    }

    return assembly;
  }

}

