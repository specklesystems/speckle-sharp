using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Reflection;
using Autodesk.Revit.UI;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB.Events;
using Revit.Async;
using CefSharp;
using System.Linq;
using System.IO;
using Speckle.Connectors.DUI.Bridge;

namespace Speckle.Connectors.Revit.Plugin;

public class RevitPlugin : IRevitPlugin
{
  private readonly UIControlledApplication _uIControlledApplication;
  private readonly RevitSettings _revitSettings;

  public RevitPlugin(UIControlledApplication uIControlledApplication, RevitSettings revitSettings)
  {
    _uIControlledApplication = uIControlledApplication;
    _revitSettings = revitSettings;
  }

  public UIApplication? UiApplication { get; private set; }

  public CefSharpPanel CefSharpPanel { get; private set; }

  public void Initialise()
  {
    _uIControlledApplication.ControlledApplication.ApplicationInitialized += OnApplicationInitialized;

    CreateTabAndRibbonPanel(_uIControlledApplication);
  }

  public void Shutdown()
  {
    // POC: should we be cleaning up he RibbonPanel etc...
    // Should we be indicating to any active in-flight functions that we are being closed?
  }

  // POC: Could be injected but maybe not worthwhile
  private void CreateTabAndRibbonPanel(UIControlledApplication application)
  {
    // POC: some TL handling and feedback here
    try
    {
      application.CreateRibbonTab(_revitSettings.RevitTabName);
    }
    catch (ArgumentException)
    {
      throw;
    }

    RibbonPanel specklePanel = application.CreateRibbonPanel(_revitSettings.RevitTabName, _revitSettings.RevitTabTitle);
    PushButton _ =
      specklePanel.AddItem(
        new PushButtonData(
          _revitSettings.RevitButtonName,
          _revitSettings.RevitButtonText,
          typeof(RevitExternalApplication).Assembly.Location,
          typeof(SpeckleRevitDui3Command).FullName
        )
      ) as PushButton;
  }

  private void OnApplicationInitialized(object sender, Autodesk.Revit.DB.Events.ApplicationInitializedEventArgs e)
  {
    // POC: not sure what this is doing...  could be messing up our Aliasing????
    AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

    UiApplication = new UIApplication(sender as Application);

    // POC: might be worth to interface this out, we shall see...
    RevitTask.Initialize(UiApplication);

    RegisterPanelAndInitializePlugin();
  }

  private Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
  {
    // POC: tight binding to files
    // what is this really doing eh?
    Assembly assembly = null;
    string name = args.Name.Split(',')[0];
    string path = Path.GetDirectoryName(typeof(RevitPlugin).Assembly.Location);

    if (path != null)
    {
      string assemblyFile = Path.Combine(path, name + ".dll");

      if (File.Exists(assemblyFile))
      {
        assembly = Assembly.LoadFrom(assemblyFile);
      }
    }

    return assembly;
  }

  private void RegisterPanelAndInitializePlugin()
  {
    CefSharpSettings.ConcurrentTaskExecution = true;

    CefSharpPanel = new CefSharpPanel();

    _uIControlledApplication.RegisterDockablePane(
      RevitExternalApplication.DoackablePanelId,
      "Speckle DUI3",
      CefSharpPanel
    );

    
    IEnumerable<BrowserBridge> bridges = Factory
      .CreateBindings(RevitDocumentStore)
      .Select(
        binding =>
          new BrowserBridge(
            CefSharpPanel.Browser,
            binding,
            CefSharpPanel.ExecuteScriptAsync,
            CefSharpPanel.ShowDevTools
          )
      );

#if REVIT2020
          // Panel.Browser.JavascriptObjectRepository.NameConverter = null; // not available in cef65, we need the below
          BindingOptions bindingOptions = new () { CamelCaseJavascriptNames = false };
#endif

#if REVIT2023
        CefSharpPanel.Browser.JavascriptObjectRepository.NameConverter = null;
        BindingOptions bindingOptions = BindingOptions.DefaultBinder;
#endif

    CefSharpPanel.Browser.IsBrowserInitializedChanged += (sender, e) =>
    {
      foreach (BrowserBridge bridge in bridges)
      {
        CefSharpPanel.Browser.JavascriptObjectRepository.Register(
          bridge.FrontendBoundName,
          bridge,
          true,
          bindingOptions
        );
      }

#if REVIT2020
          // NOTE: Cef65 does not work with DUI3 in yarn dev mode. To test things you need to do `yarn build` and serve the build
          // folder at port 3000 (or change it to something else if you want to). Guru  meditation: Je sais, pas ideal. Mais q'est que nous pouvons faire? Rien. C'est l'autodesk vie.
          // NOTE: To run the ui from a build, follow these steps:
          // - run `yarn build` in the DUI3 folder
          // - run ` PORT=3003  node .output/server/index.mjs` after the build

          CefSharpPanel.Browser.Load("http://localhost:3003");
          CefSharpPanel.Browser.ShowDevTools();
#endif

#if REVIT2023
          CefSharpPanel.Browser.Load("http://localhost:8082");
#endif
    };
    */
  }
}
