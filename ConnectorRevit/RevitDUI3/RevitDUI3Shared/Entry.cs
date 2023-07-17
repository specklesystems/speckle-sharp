using System;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.UI;

namespace Speckle.ConnectorRevitDUI3;

public class App : IExternalApplication
{
  public static UIApplication AppInstance { get; set; }

  public static UIControlledApplication UICtrlApp { get; set; }

  public Result OnStartup(UIControlledApplication application)
  {
    
    UICtrlApp = application;
    UICtrlApp.ControlledApplication.ApplicationInitialized += (sender, e) =>
    {
      AppInstance = new UIApplication(sender as Application);
    };
    
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
    RegisterDockablePane(UICtrlApp);  
    return Result.Succeeded;
  }
  
  internal static DockablePaneId PanelId = new DockablePaneId(new Guid("{85F73DA4-3EF4-4870-BDBC-FD2D238EED31}"));
  public static Panel Panel { get; set; }
  
  public void RegisterDockablePane(UIControlledApplication application)
  {
    Panel = new Panel();
    application.RegisterDockablePane(PanelId, "Speckle DUI3", Panel); 
  }

  public Result OnShutdown(UIControlledApplication application)
  {
    return Result.Succeeded;
  }

}

