using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.Revit.UI;

namespace Speckle.Connectors.Revit.Plugin;

internal class RevitPlugin : IRevitPlugin
{
  private readonly UIControlledApplication _uIControlledApplication;
  private readonly RevitSettings _revitSettings;

  internal RevitPlugin(UIControlledApplication uIControlledApplication, RevitSettings revitSettings)
  {
    _uIControlledApplication = uIControlledApplication;
    _revitSettings = revitSettings;
  }

  public void Initialise()
  {
    _uIControlledApplication.ControlledApplication.ApplicationInitialized +=
      ControlledApplication_ApplicationInitialized;
  }

  public void Shutdown() { }

  private void CreateTabAndRibbonPanel(UIControlledApplication application)
  {
    string tabName = _revitSettings.RevitPanelName;

    // POC: some TL handling and feedback here
    try
    {
      application.CreateRibbonTab(tabName);
    }
    catch (ArgumentException)
    {
      throw;
    }

    RibbonPanel specklePanel = application.CreateRibbonPanel(tabName, "Speckle 2 DUI3");
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

  private void ControlledApplication_ApplicationInitialized(
    object sender,
    Autodesk.Revit.DB.Events.ApplicationInitializedEventArgs e
  )
  {
    int t = -1;
  }
}
