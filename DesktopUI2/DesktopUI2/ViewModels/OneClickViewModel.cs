using Avalonia.Controls;
using DesktopUI2.Models;
using DesktopUI2.Views.Windows.Dialogs;
using ReactiveUI;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace DesktopUI2.ViewModels
{
  public class OneClickViewModel : ReactiveObject
  {
    List<StreamState> SavedStreams { get; set; } = new List<StreamState>();

    public ConnectorBindings Bindings { get; private set; } = new DummyBindings();

    public bool HasFileStream => FileStream != null;

    private string _fileName;

    private StreamState _fileStream;
    public StreamState FileStream
    {
      get => _fileStream;
      set
      {
        this.RaiseAndSetIfChanged(ref _fileStream, value);
        this.RaisePropertyChanged("HasFileStream");
      }
    }

    public OneClickViewModel(ConnectorBindings _bindings, StreamState fileStream = null)
    {
      Bindings = _bindings;
      SavedStreams = Bindings.GetStreamsInFile();
      _fileName = Bindings.GetFileName();

      if (fileStream == null)
        LoadFileStream();
      else
        FileStream = fileStream;
    }

    private void LoadFileStream()
    {
      // get default account
      var account = AccountManager.GetDefaultAccount();
      var client = new Client(account);

      var fileStream = SavedStreams.Where(o => o.CachedStream.name == _fileName && o.Client.Account == account)?.FirstOrDefault();
      if (fileStream != null)
      {
        FileStream = fileStream;
      }
      else // try to find stream in account
      {
        Stream foundStream = Task.Run(async () => await SearchStreams(client)).Result;

        if (foundStream == null) // create the stream
        {
          var description = $"Automatic stream for {_fileName}";
          try
          {
            string streamId = Task.Run(async () => await client.StreamCreate(new StreamCreateInput { description = description, name = _fileName, isPublic = false })).Result;
            foundStream = Task.Run(async () => await client.StreamGet(streamId)).Result;
          }
          catch (Exception e) { }
        }

        FileStream = new StreamState(account, foundStream) { BranchName = "main" };
        SavedStreams.Add(FileStream);
        Bindings.WriteStreamsToFile(SavedStreams);

        if (HomeViewModel.Instance != null)
          HomeViewModel.Instance.UpdateSavedStreams(SavedStreams);
      }
    }

    private async Task<Stream> SearchStreams(Client client)
    {
      Stream stream = null;
      try
      {
        var streams = await client.StreamSearch(_fileName);
        stream = streams.Where(s => s.name.Equals(_fileName))?.FirstOrDefault();
      }
      catch (Exception) { }
      return stream;
    }

    public async void Send()
    {
      // check if objs are selected and set streamstate filter
      var filters = Bindings.GetSelectionFilters();
      var selection = Bindings.GetSelectedObjects();
      if (selection.Count > 0)
      {
        FileStream.Filter = filters.Where(o => o.Slug == "manual").First();
        FileStream.Filter.Selection = selection;
        FileStream.CommitMessage = "Sent selection";
      }
      else
      {
        FileStream.Filter = filters.Where(o => o.Slug == "all").First();
        FileStream.CommitMessage = "Sent everything";
      }
      FileStream.BranchName = "main";

      // set settings
      if (FileStream.Settings == null || FileStream.Settings.Count == 0)
      {
        var settings = Bindings.GetSettings();
        FileStream.Settings = settings;
      }

      // send to stream
      try
      {
        var progress = new ProgressViewModel();
        progress.ProgressTitle = "Sending to Speckle 🚀";
        progress.IsProgressing = true;

        var dialog = new QuickOpsDialog();
        dialog.DataContext = progress;
        dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        dialog.Show();

        await Task.Run(() => Bindings.SendStream(FileStream, progress));
        progress.IsProgressing = false;
        dialog.Close();
        if (!progress.CancellationTokenSource.IsCancellationRequested)
        {
          Analytics.TrackEvent(AccountManager.GetDefaultAccount(), Analytics.Events.Send, new Dictionary<string, object> { { "method", "OneClick" } });
          FileStream.LastUsed = DateTime.Now.ToString();

          // open in browser
          if (FileStream.PreviousCommitId != null)
          {
            string commitUrl = $"{FileStream.ServerUrl.TrimEnd('/')}/streams/{FileStream.StreamId}/commits/{FileStream.PreviousCommitId}";
            Process.Start(new ProcessStartInfo(commitUrl) { UseShellExecute = true });
          }
        }
      }
      catch (Exception ex)
      { }

      if (Bindings.UpdateSavedStreams != null)
        Bindings.UpdateSavedStreams(SavedStreams);

      if (HomeViewModel.Instance != null)
        HomeViewModel.Instance.UpdateSavedStreams(SavedStreams);
    }
  }
}
