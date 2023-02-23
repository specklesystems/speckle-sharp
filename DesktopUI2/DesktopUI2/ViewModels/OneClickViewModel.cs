﻿using Avalonia.Controls;
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
using Serilog;

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

    private string _fileName;

    private StreamState _fileStream;


    private bool _sendClicked;
    public bool SendClicked
    {
      get => _sendClicked;
      set => this.RaiseAndSetIfChanged(ref _sendClicked, value);

    }

    private string _sentText;
    public string SentText
    {
      get => _sentText;
      set => this.RaiseAndSetIfChanged(ref _sentText, value);

    }

    private bool _successfulSend;
    public bool SuccessfulSend
    {
      get => _successfulSend;
      set => this.RaiseAndSetIfChanged(ref _successfulSend, value);

    }

    private string Url
    {
      get
      {
        var commit = "";
        if (!string.IsNullOrEmpty(Id))
          commit = "commits/" + Id;
        return $"{_fileStream.ServerUrl.TrimEnd('/')}/streams/{_fileStream.StreamId}/{commit}";
      }
    }

    #endregion

    private string Id { get; set; }

    public OneClickViewModel(IScreen screen)
    {
      try
      {
        HostScreen = screen;
        Bindings = Locator.Current.GetService<ConnectorBindings>();
      }
      catch (Exception ex)
      {
        Log.Fatal(ex, "Could not initialize one click screen {exceptionMessage}",ex.Message);
      }
    }

    public async void Send()
    {
      SendClicked = true;
      Progress = new ProgressViewModel();
      Progress.ProgressTitle = "Sending to Speckle 🚀...";
      Progress.IsProgressing = true;


      string fileName = null;
      try
      {
        fileName = Bindings.GetFileName();
      }
      catch(Exception ex)
      {
        //todo: handle properly in each connector bindings
        Log.Warning(ex, "Swallowing exception in {methodName}: {exceptionMessage}", nameof(Send), ex.Message);
      }

      //filename is different, might have been renamed or be a different document
      if (_fileName != fileName)
        _fileStream = null;

      _fileName = fileName;

      if (_fileStream == null)
        _fileStream = await GetOrCreateStreamState();
      // check if objs are selected and set streamstate filter
      var filters = Bindings.GetSelectionFilters();
      var selection = Bindings.GetSelectedObjects();
      //TODO: check if these filters exist
      if (selection.Count > 0)
      {
        _fileStream.Filter = filters.First(o => o.Slug == "manual");
        _fileStream.Filter.Selection = selection;
        _fileStream.CommitMessage = "Sent selection";
      }
      else
      {
        _fileStream.Filter = filters.First(o => o.Slug == "all");
        _fileStream.CommitMessage = "Sent everything";
      }
      _fileStream.BranchName = "main";

      // set settings
      if (_fileStream.Settings == null || _fileStream.Settings.Count == 0)
      {
        var settings = Bindings.GetSettings();
        _fileStream.Settings = settings;
      }

      // send to stream
      // TODO: report conversions errors, empty commit etc
      try
      {
        Id = await Task.Run(() => Bindings.SendStream(_fileStream, Progress));

        if (!string.IsNullOrEmpty(Id))
        {
          var errorsCount = Progress.Report.ReportObjects.Values.Count(x => x.Status == Speckle.Core.Models.ApplicationObject.State.Failed);
          if (errorsCount > 0)
          {
            var s = errorsCount == 1 ? "" : "s";
            SentText = $"Done 🎉\nOperation completed with {errorsCount} error{s}";
            SuccessfulSend = true;
          }
          else if (Progress.Report.ReportObjects.Count == 0)
          {
            SentText = "Nothing was sent!\nPlease try again or switch to advanced mode.";
            SuccessfulSend = false;
          }
          else
          {
            SentText = "Done 🎉\nYour model has been sent!";
            SuccessfulSend = true;
          }
        }
        else
        {
          SentText = "Something went wrong!\nPlease try again or switch to advanced mode.";
          SuccessfulSend = false;
        }

        Progress.IsProgressing = false;

        if (!Progress.CancellationTokenSource.IsCancellationRequested)
        {
          Analytics.TrackEvent(AccountManager.GetDefaultAccount(), Analytics.Events.Send, new Dictionary<string, object> { { "method", "OneClick" } });
          _fileStream.LastUsed = DateTime.Now.ToString();
        }
      }
      catch (Exception ex)
      {
        Log.Error(ex, "Could not send with one click {exceptionMessage}",ex.Message);
      }

      if (HomeViewModel.Instance != null)
        HomeViewModel.Instance.AddSavedStream(new StreamViewModel(_fileStream, HostScreen, HomeViewModel.Instance.RemoveSavedStreamCommand));
    }

    private async Task<StreamState> GetOrCreateStreamState()
    {
      // get default account
      var account = AccountManager.GetDefaultAccount();
      var client = new Client(account);

      SavedStreams = Bindings.GetStreamsInFile();
      var fileStream = SavedStreams.Where(o => o.CachedStream.name == _fileName && o.Client.Account == account)?.FirstOrDefault();
      if (fileStream != null)
      {
        return fileStream;
      }
      // try to find stream in account
      var foundStream = await SearchStreams(client);

      if (foundStream != null)
      {
        return new StreamState(account, foundStream) { BranchName = "main" };
      }

      // create the stream
      string streamId = await client.StreamCreate(new StreamCreateInput { description = "Autogenerated Stream for QuickSend", name = _fileName, isPublic = false });
      var newStream = await client.StreamGet(streamId);

      return new StreamState(account, newStream) { BranchName = "main" };


    }

    private async Task<Stream> SearchStreams(Client client)
    {
      Stream stream = null;
      try
      {
        var streams = await client.StreamSearch(_fileName);
        stream = streams.FirstOrDefault(s => s.name == _fileName);
      }
      catch (Exception ex)
      {
        Log.ForContext("fileName", _fileName)
          .Debug(ex, "Swallowing exception in {methodName}: {exceptionMessage}", nameof(SearchStreams), ex.Message);
      }
      return stream;
    }

    private void ShareCommand()
    {
      MainViewModel.RouterInstance.Navigate.Execute(new CollaboratorsViewModel(HostScreen, new StreamViewModel(_fileStream, HostScreen, null)));

      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Share Open" } });
    }

    private void CloseSendModal()
    {
      if (Progress.IsProgressing)
      {
        Progress.CancelCommand();
      }
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
      var config = ConfigManager.Load();
      config.OneClickMode = false;
      ConfigManager.Save(config);

      MainViewModel.Instance.NavigateToDefaultScreen();
    }


  }
}
