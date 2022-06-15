using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Speckle.ConnectorRevit.UI;

namespace Speckle.ConnectorRevit.Entry
{
  [Transaction(TransactionMode.Manual)]
  public class OneClickSendCommand : IExternalCommand
  {
    public static ConnectorBindingsRevit2 Bindings { get; set; }
    public static StreamState FileStream { get; set; }

    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
      // intialize dui2
      SpeckleRevitCommand2.CreateOrFocusSpeckle(false);

      // send
      var oneClick = new OneClickViewModel(Bindings, FileStream);
      oneClick.Send();
      FileStream = oneClick.FileStream;

      return Result.Succeeded;
    }

  }

}
