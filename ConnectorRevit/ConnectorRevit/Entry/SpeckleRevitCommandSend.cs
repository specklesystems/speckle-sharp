using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using DesktopUI2.Models;
using DesktopUI2.ViewModels;

using Speckle.ConnectorRevit.Storage;
using Speckle.ConnectorRevit.UI;

using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Revit.Async;

namespace Speckle.ConnectorRevit.Entry
{
  [Transaction(TransactionMode.Manual)]
  public class SpeckleRevitCommandSend : IExternalCommand
  {
    public static ConnectorBindingsRevit2 Bindings { get; set; }
    private static UIApplication uiapp;
    public static UIDocument doc;
    private static StreamState _stream { get; set; }

    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
      uiapp = commandData.Application;
      doc = uiapp.ActiveUIDocument;

      if (_stream == null)
      {
        var stream = Task.Run(async () => await CreateOrRetrieveStreamAsync()).Result;
        _stream = stream;
      }

      // check if objs are selected and set streamstate filter
      var filters = Bindings.GetSelectionFilters();
      var selection = Bindings.GetSelectedObjects();
      if (selection.Count > 0)
      {
        _stream.Filter = filters.Where(o => o.Slug == "manual").First();
        _stream.Filter.Selection = selection;
        _stream.CommitMessage = "Sent selection";
      }
      else
      {
        _stream.Filter = filters.Where(o => o.Slug == "all").First();
        _stream.CommitMessage = "Sent everything";
      }

      // set settings
      if (_stream.Settings == null)
      {
        var settings = Bindings.GetSettings();
        _stream.Settings = settings;
      }

      // send to stream
      var result = Task.Run(async () => await SendAsync()).Result;

      // open up browser with send
      string commitUrl = $"{_stream.ServerUrl.TrimEnd('/')}/streams/{_stream.StreamId}/commits/{result}"; ;
      Process.Start(commitUrl);

      return Result.Succeeded;
    }

    private void MainWindow_StateChanged(object sender, EventArgs e)
    {
    }
    private async Task<string> SendAsync()
    {
      var Progress = new ProgressViewModel();
      Progress.IsProgressing = true;
      await Task.Run(() => Bindings.SendStream(_stream, Progress));
      return _stream.PreviousCommitId;
    }
    public async void WriteStreamsToFile(List<StreamState> streams)
    {
      await RevitTask.RunAsync(
        app =>
        {
          using (Transaction t = new Transaction(doc.Document, "Speckle Write State"))
          {
            t.Start();
            StreamStateManager2.WriteStreamStateList(doc.Document, streams);
            t.Commit();
          }

        });
    }
    public async Task<StreamState> CreateOrRetrieveStreamAsync()
    {
      // get name of this file: this is the name of the stream that will be created
      var name = doc.Document.Title;
      var account = AccountManager.GetDefaultAccount();
      var client = new Client(account);

      // see if this stream exists in this file already
      var streams = StreamStateManager2.ReadState(doc.Document);
      StreamState foundStreamState = (streams.Count > 0) ? streams?.Where(o => o.CachedStream.name == name && o.UserId == account.userInfo.id)?.First() : null;

      if (foundStreamState == null)
      {
        // try to find stream of this name on default user account
        Stream foundStream = null;
        var accountStreams = await client.StreamsGet();
        if (accountStreams.Count > 0) foundStream = accountStreams.Where(o => o.name == name)?.First();

        if (foundStream == null)
        {
          // create new stream 
          var description = $"Automatic stream for {name}";
          try
          {
            var streamId = await client.StreamCreate(new StreamCreateInput { description = description, name = name, isPublic = false });
            foundStream = await client.StreamGet(streamId);
          }
          catch (Exception e)
          { }
        }

        foundStreamState = new StreamState(account, foundStream);
        foundStreamState.BranchName = "main";
        streams.Add(foundStreamState);
        WriteStreamsToFile(streams);
      }

      return foundStreamState;
    }

  }

}
