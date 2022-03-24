using Autodesk.AutoCAD.Runtime;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

using DesktopUI2.ViewModels;
using DesktopUI2.Models;
using Speckle.ConnectorAutocadCivil.UI;


namespace Speckle.ConnectorAutocadCivil.Entry
{
  public class OneClickCommand
  {
    public static ConnectorBindingsAutocad Bindings { get; set; }
    public static StreamState FileStream { get; set; }

    /// <summary>
    /// Command to send selection to the document stream, or everything if nothing is selected
    /// </summary>
    [CommandMethod("SpeckleSend", CommandFlags.Modal)]
    public static void SendCommand()
    {
      // initialize dui
      SpeckleAutocadCommand.CreateOrFocusSpeckle(false);

      // send
      var oneClick = new OneClickViewModel(Bindings, FileStream);
      oneClick.Send();
      FileStream = oneClick.FileStream;
    }
  }
}

