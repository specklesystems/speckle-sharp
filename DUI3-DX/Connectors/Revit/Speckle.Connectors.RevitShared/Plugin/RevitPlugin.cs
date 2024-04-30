using System;
using System.Collections.Generic;
using Autodesk.Revit.UI;
using Autodesk.Revit.ApplicationServices;
using Revit.Async;
using CefSharp;
using System.Linq;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Bindings;
using System.Diagnostics;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Connectors.Revit.Plugin;

internal class RevitPlugin : IRevitPlugin
{
  private readonly UIControlledApplication _uIControlledApplication;
  private readonly RevitSettings _revitSettings;
  private readonly IEnumerable<Lazy<IBinding>> _bindings; // should be lazy to ensure the bindings are not created too early
  private readonly BindingOptions _bindingOptions;
  private readonly CefSharpPanel _panel;
  private readonly RevitContext _revitContext;
  private readonly CefSharpPanel _cefSharpPanel;

  public RevitPlugin(
    UIControlledApplication uIControlledApplication,
    RevitSettings revitSettings,
    IEnumerable<Lazy<IBinding>> bindings,
    BindingOptions bindingOptions,
    RevitContext revitContext,
    CefSharpPanel cefSharpPanel
  )
  {
    _uIControlledApplication = uIControlledApplication;
    _revitSettings = revitSettings;
    _bindings = bindings;
    _bindingOptions = bindingOptions;
    _revitContext = revitContext;
    _cefSharpPanel = cefSharpPanel;
  }

  public void Initialise()
  {
    _uIControlledApplication.ControlledApplication.ApplicationInitialized += OnApplicationInitialized;

    CreateTabAndRibbonPanel(_uIControlledApplication);
  }

  public void Shutdown()
  {
    // POC: should we be cleaning up the RibbonPanel etc...
    // Should we be indicating to any active in-flight functions that we are being closed?
  }

  // POC: Could be injected but maybe not worthwhile
  private void CreateTabAndRibbonPanel(UIControlledApplication application)
  {
    // POC: some top-level handling and feedback here
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
          typeof(SpeckleRevitCommand).FullName
        )
      ) as PushButton;
  }

  private void OnApplicationInitialized(object sender, Autodesk.Revit.DB.Events.ApplicationInitializedEventArgs e)
  {
    var uiApplication = new UIApplication(sender as Application);
    _revitContext.UIApplication = uiApplication;

    // POC: might be worth to interface this out, we shall see...
    RevitTask.Initialize(uiApplication);

    RegisterPanelAndInitializePlugin();
  }

  private void RegisterPanelAndInitializePlugin()
  {
    CefSharpSettings.ConcurrentTaskExecution = true;

    _uIControlledApplication.RegisterDockablePane(
      RevitExternalApplication.DoackablePanelId,
      _revitSettings.RevitPanelName,
      _cefSharpPanel
    );

    // binding the bindings to each bridge
    foreach (IBinding binding in _bindings.Select(x => x.Value))
    {
      Debug.WriteLine(binding.Name);
      binding.Parent.AssociateWithBinding(binding, _cefSharpPanel.Browser.ExecuteScriptAsync, _cefSharpPanel);
    }

    _cefSharpPanel.Browser.IsBrowserInitializedChanged += (sender, e) =>
    {
      // POC dev tools
      _cefSharpPanel.ShowDevTools();

      foreach (IBinding binding in _bindings.Select(x => x.Value))
      {
        IBridge bridge = binding.Parent;

        _cefSharpPanel.Browser.JavascriptObjectRepository.Register(
          bridge.FrontendBoundName,
          bridge,
          true,
          _bindingOptions
        );
      }

      _cefSharpPanel.Browser.Load("https://boisterous-douhua-e3cefb.netlify.app/");

      // POC: not sure where this comes from
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
  }
}
