using Autodesk.Revit.UI;
using Speckle.Core;

namespace Speckle.ConnectorRevitDUI3;

  public class App: IExternalApplication
  {
    public static UIApplication AppInstance { get; set; }

    public static UIControlledApplication UICtrlApp { get; set; }
    
    public Result OnStartup(UIControlledApplication application)
    {
      UICtrlApp = application;
      
      string tabName = "Speckle";
      try
      {
        application.CreateRibbonTab(tabName);
      }
      catch { }

      var specklePanel = application.CreateRibbonPanel(tabName, "Speckle 2 DUI3");
      var speckleButton = specklePanel.AddItem(new PushButtonData("Speckle 2 DUI3", "Revit Connector", typeof(App).Assembly.Location, typeof(SpeckleRevitDUI3Command).FullName)) as PushButton;
      
      return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application)
    {
      return Result.Succeeded;
    }
    
  }

