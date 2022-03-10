using System.Diagnostics;
using System.Threading.Tasks;
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

    private static StreamState _stream { get; set; }

    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
      var viewModel = new OneClickViewModel(Bindings, _stream);
      viewModel.OneClickSend();

      _stream = viewModel.FileStream;

      // open up browser with send
      if (_stream.PreviousCommitId != null)
      {
        string commitUrl = $"{_stream.ServerUrl.TrimEnd('/')}/streams/{_stream.StreamId}/commits/{_stream.PreviousCommitId}";
        Process.Start(commitUrl);
      }

      return Result.Succeeded;
    }

  }

}
