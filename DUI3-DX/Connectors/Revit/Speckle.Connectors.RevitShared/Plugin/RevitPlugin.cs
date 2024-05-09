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
using Speckle.Core.Logging;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.IO;

namespace Speckle.Connectors.Revit.Plugin;

internal class RevitPlugin : IRevitPlugin
{
  private readonly UIControlledApplication _uIControlledApplication;
  private readonly RevitSettings _revitSettings;
  private readonly IEnumerable<Lazy<IBinding>> _bindings; // should be lazy to ensure the bindings are not created too early
  private readonly BindingOptions _bindingOptions;
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
    // Create and register panels before app initialized. this was needed for double-click file open
    CreateTabAndRibbonPanel(_uIControlledApplication);
    RegisterPanelAndInitializePlugin();
    _uIControlledApplication.ControlledApplication.ApplicationInitialized += OnApplicationInitialized;
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
      // exception occurs when the speckle tab has already been created.
      // this happens when both the dui2 and the dui3 connectors are installed. Can be safely ignored.
    }

    RibbonPanel specklePanel = application.CreateRibbonPanel(_revitSettings.RevitTabName, _revitSettings.RevitTabTitle);
    PushButton dui3Button =
      specklePanel.AddItem(
        new PushButtonData(
          _revitSettings.RevitButtonName,
          _revitSettings.RevitButtonText,
          typeof(RevitExternalApplication).Assembly.Location,
          typeof(SpeckleRevitCommand).FullName
        )
      ) as PushButton;

    string path = typeof(RevitPlugin).Assembly.Location;
    dui3Button.Image = LoadPngImgSource(
      $"Speckle.Connectors.Revit{_revitSettings.RevitVersionName}.Assets.logo16.png",
      path
    );
    dui3Button.LargeImage = LoadPngImgSource(
      $"Speckle.Connectors.Revit{_revitSettings.RevitVersionName}.Assets.logo32.png",
      path
    );
    dui3Button.ToolTipImage = LoadPngImgSource(
      $"Speckle.Connectors.Revit{_revitSettings.RevitVersionName}.Assets.logo32.png",
      path
    );
    dui3Button.ToolTip = "Speckle Connector for Revit New UI";
    //dui3Button.AvailabilityClassName = typeof(CmdAvailabilityViews).FullName;
    dui3Button.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://speckle.systems"));
  }

  private void OnApplicationInitialized(object sender, Autodesk.Revit.DB.Events.ApplicationInitializedEventArgs e)
  {
    var uiApplication = new UIApplication(sender as Application);
    _revitContext.UIApplication = uiApplication;

    // POC: might be worth to interface this out, we shall see...
    RevitTask.Initialize(uiApplication);

    PostApplicationInit(); // for double-click file open
  }

  /// <summary>
  /// Actions to run after UiApplication initialized. This was needed for double-click file open issue.
  /// </summary>
  private void PostApplicationInit()
  {
    // binding the bindings to each bridge
    foreach (IBinding binding in _bindings.Select(x => x.Value))
    {
      Debug.WriteLine(binding.Name);
      binding.Parent.AssociateWithBinding(
        binding,
        _cefSharpPanel.ExecuteScriptAsync,
        _cefSharpPanel,
        _cefSharpPanel.ShowDevTools
      );
    }

    _cefSharpPanel.Browser.IsBrowserInitializedChanged += (sender, e) =>
    {
      // Not needed now, as we should be able to correctly open dev tools via user interaction
      // _cefSharpPanel.ShowDevTools();

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

      // POC: Below line seems unneccesary but not removing just in case we did it like this? Maybe check it later
      //  with some other revit connectors again since CefSharp version is different
      // _cefSharpPanel.Browser.Load("https://boisterous-douhua-e3cefb.netlify.app/");

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

  private void RegisterPanelAndInitializePlugin()
  {
    CefSharpSettings.ConcurrentTaskExecution = true;

    _uIControlledApplication.RegisterDockablePane(
      RevitExternalApplication.DoackablePanelId,
      _revitSettings.RevitPanelName,
      _cefSharpPanel
    );
  }

  private ImageSource? LoadPngImgSource(string sourceName, string path)
  {
    try
    {
      var assembly = Assembly.LoadFrom(Path.Combine(path));
      var icon = assembly.GetManifestResourceStream(sourceName);
      PngBitmapDecoder decoder = new(icon, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
      ImageSource source = decoder.Frames[0];
      return source;
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      // POC: logging
    }

    return null;
  }
}
