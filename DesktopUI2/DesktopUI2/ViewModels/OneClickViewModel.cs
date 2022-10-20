using Avalonia.Controls;
using DesktopUI2.Models;
using DesktopUI2.Views.Windows.Dialogs;
using ReactiveUI;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Splat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace DesktopUI2.ViewModels
{
  public class OneClickViewModel : ReactiveObject, IRoutableViewModel
  {
    public IScreen HostScreen { get; }

    public string UrlPathSegment { get; } = "oneclick";


    public ConnectorBindings Bindings { get; private set; } = new DummyBindings();

    #region bindings

    public string Title => "for " + Bindings.GetHostAppNameVersion();
    public string Version => "v" + Bindings.ConnectorVersion;
    List<StreamState> SavedStreams { get; set; } = new List<StreamState>();

    private ProgressViewModel _progress;
    public ProgressViewModel Progress
    {
      get => _progress;
      set => this.RaiseAndSetIfChanged(ref _progress, value);

    }

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

    private bool _sendComplete;
    public bool SendComplete
    {
      get => _sendComplete;
      set => this.RaiseAndSetIfChanged(ref _sendComplete, value);

    }

    private bool _sendClicked;
    public bool SendClicked
    {
      get => _sendClicked;
      set => this.RaiseAndSetIfChanged(ref _sendClicked, value);

    }

    private string Url
    {
      get
      {
        var commit = "";
        if (!string.IsNullOrEmpty(Id))
          commit = "commits/" + Id;
        return $"{FileStream.ServerUrl.TrimEnd('/')}/streams/{FileStream.StreamId}/{commit}";
      }
    }

    #endregion

    private string Id { get; set; }
    private List<AccountViewModel> Users { get; set; }

    public OneClickViewModel(IScreen screen)
    {
      try
      {
        HostScreen = screen;
        Bindings = Locator.Current.GetService<ConnectorBindings>();
      }
      catch (Exception ex)
      {
        Log.CaptureException(ex, Sentry.SentryLevel.Error);
      }
    }

    public async void Send()
    {
      SendClicked = true;

      SavedStreams = Bindings.GetStreamsInFile();

      if (FileStream == null)
        LoadFileStream();
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
        Progress = new ProgressViewModel();
        Progress.ProgressTitle = "Sharing via Speckle 🚀";
        Progress.IsProgressing = true;

        Id = await Bindings.SendStream(FileStream, Progress);
        Progress.IsProgressing = false;

        if (!Progress.CancellationTokenSource.IsCancellationRequested)
        {
          Analytics.TrackEvent(AccountManager.GetDefaultAccount(), Analytics.Events.Send, new Dictionary<string, object> { { "method", "OneClick" } });
          FileStream.LastUsed = DateTime.Now.ToString();

          SendComplete = true;
        }
        //else
        //  ShareViewModel.RouterInstance.NavigateBack.Execute();
      }
      catch (Exception ex)
      { }

      if (Bindings.UpdateSavedStreams != null)
        Bindings.UpdateSavedStreams(SavedStreams);

      if (HomeViewModel.Instance != null)
        HomeViewModel.Instance.UpdateSavedStreams(SavedStreams);
    }

    private void LoadFileStream()
    {
      // get default account
      var account = AccountManager.GetDefaultAccount();
      var client = new Client(account);
      try
      {
        _fileName = Bindings.GetFileName();
      }
      catch
      {
        _fileName = "Unnamed stream";
        //todo: handle properly in each connector bindings
      }

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
        stream = streams.FirstOrDefault(s => s.name == _fileName);
      }
      catch (Exception) { }
      return stream;
    }

    private void CloseSendModal()
    {
      SendClicked = false;
    }

    private void ViewOnlineCommand()
    {


      Process.Start(new ProcessStartInfo(Url) { UseShellExecute = true });
    }

    private void CopyCommand()
    {
      Avalonia.Application.Current.Clipboard.SetTextAsync(Url);
    }

    private void AdvancedModeCommand()
    {
      MainViewModel.RouterInstance.Navigate.Execute(new HomeViewModel(HostScreen));

      var config = ConfigManager.Load();
      config.OneClickMode = false;
      ConfigManager.Save(config);
    }


  }
}
