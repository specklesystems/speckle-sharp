using DesktopUI2.Models;
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

namespace DesktopUI2.ViewModels.Share
{
  public class SendingViewModel : ReactiveObject, IRoutableViewModel
  {
    public IScreen HostScreen { get; }
    public string UrlPathSegment { get; } = "sending";

    private ConnectorBindings Bindings;

    #region bindings
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

    public SendingViewModel(IScreen screen, List<AccountViewModel> users)
    {
      HostScreen = screen;
      Bindings = Locator.Current.GetService<ConnectorBindings>();
      Users = users;
      Start();
    }

    private async void Start()
    {
      await SetupStream();
      await Send();
      await AddCollaborators();
    }

    private async Task SetupStream()
    {
      SavedStreams = Bindings.GetStreamsInFile();
      _fileName = Bindings.GetFileName();

      // get default account
      var account = AccountManager.GetDefaultAccount();
      var client = new Client(account);

      var fileStream = SavedStreams.FirstOrDefault(o => o.CachedStream.name == _fileName && o.Client.Account == account);
      if (fileStream != null)
      {
        FileStream = fileStream;
      }
      else // try to find stream in account
      {
        var foundStream = await SearchStreams(client);

        if (foundStream == null) // create the stream
        {
          var description = $"Sharing {_fileName} via Speckle!";
          try
          {
            string streamId = await client.StreamCreate(new StreamCreateInput { description = description, name = _fileName, isPublic = false });
            foundStream = await client.StreamGet(streamId);
          }
          catch (Exception e)
          {
            Dialogs.ShowDialog("Could not share", "There was an error creating the Stream: " + e.Message, Material.Dialog.Icons.DialogIconKind.Error);
            return;
          }
        }


        FileStream = new StreamState(account, foundStream) { BranchName = "main" };
        SavedStreams.Add(FileStream);
        Bindings.WriteStreamsToFile(SavedStreams);

        if (HomeViewModel.Instance != null)
          HomeViewModel.Instance.UpdateSavedStreams(SavedStreams);

      }
    }

    private async Task AddCollaborators()
    {
      foreach (var user in Users)
      {
        if (Utils.IsValidEmail(user.Name))
        {
          try
          {
            await FileStream.Client.StreamInviteCreate(new StreamInviteCreateInput { email = user.Name, streamId = FileStream.StreamId, message = "I would like to share a model with you via Speckle!" });
          }
          catch (Exception e)
          {

          }
        }
        else
        {
          try
          {
            await FileStream.Client.StreamGrantPermission(new StreamGrantPermissionInput { userId = user.Id, streamId = FileStream.StreamId, role = "stream:contributor" });
          }
          catch (Exception e)
          {

          }
        }
      }
    }

    private async Task Send()
    {


      //SEND
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
          Analytics.TrackEvent(AccountManager.GetDefaultAccount(), Analytics.Events.Send, new Dictionary<string, object> { { "method", "QuickShare" } });
          FileStream.LastUsed = DateTime.Now.ToString();

          SendComplete = true;
        }
        else
          ShareViewModel.RouterInstance.NavigateBack.Execute();
      }
      catch (Exception ex)
      { }

      if (Bindings.UpdateSavedStreams != null)
        Bindings.UpdateSavedStreams(SavedStreams);

      if (HomeViewModel.Instance != null)
        HomeViewModel.Instance.UpdateSavedStreams(SavedStreams);

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

    private void ViewOnlineCommand()
    {


      Process.Start(new ProcessStartInfo(Url) { UseShellExecute = true });
    }

    private void CopyCommand()
    {
      Avalonia.Application.Current.Clipboard.SetTextAsync(Url);
    }



  }
}
