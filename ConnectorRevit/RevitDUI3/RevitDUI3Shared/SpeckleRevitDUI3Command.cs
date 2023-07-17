using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CefSharp;

namespace Speckle.ConnectorRevitDUI3;

[Transaction(TransactionMode.Manual)]
public class SpeckleRevitDUI3Command: IExternalCommand
{
  public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
  {
    var panel = commandData.Application.GetDockablePane(App.PanelId);
    panel.Show();
    
    App.Panel.Browser.ShowDevTools();
    App.Panel.Browser.Load("http://localhost:3000/");
    return Result.Succeeded;
  }
}
