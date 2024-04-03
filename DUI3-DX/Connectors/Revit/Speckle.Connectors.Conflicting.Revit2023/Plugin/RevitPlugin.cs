using System;
using Autodesk.Revit.UI;
using Autodesk.Revit.ApplicationServices;
using Serilog.Events;
using System.Diagnostics;

namespace Speckle.Connectors.Conflicting.Revit2023.Plugin;

internal class RevitPlugin
{
  private readonly UIControlledApplication _uIControlledApplication;

  public RevitPlugin(UIControlledApplication uIControlledApplication)
  {
    _uIControlledApplication = uIControlledApplication;
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
      application.CreateRibbonTab("Freckle");
    }
    catch (ArgumentException)
    {
      throw;
    }

    RibbonPanel specklePanel = application.CreateRibbonPanel("Freckle", "FreckleTab");
    //PushButton _ =
    //  specklePanel.AddItem(
    //    new PushButtonData(
    //      _revitSettings.RevitButtonName,
    //      _revitSettings.RevitButtonText,
    //      typeof(RevitExternalApplication).Assembly.Location,
    //      typeof(SpeckleRevitCommand).FullName
    //    )
    //  ) as PushButton;
  }

  private void OnApplicationInitialized(object sender, Autodesk.Revit.DB.Events.ApplicationInitializedEventArgs e)
  {
    var uiApplication = new UIApplication(sender as Application);

    RegisterPanelAndInitializePlugin();
  }

  private void RegisterPanelAndInitializePlugin()
  {
    // levelAlias off was added in version 3
    var x = LevelAlias.Off;
    Trace.WriteLine(x);
  }
}
