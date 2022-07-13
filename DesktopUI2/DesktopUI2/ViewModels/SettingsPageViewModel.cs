using DesktopUI2.Models;
using DesktopUI2.Views.Windows.Dialogs;
using ReactiveUI;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;

namespace DesktopUI2.ViewModels
{
  public class SettingsPageViewModel : ReactiveObject, IRoutableViewModel
  {

    public IScreen HostScreen { get; }
    public string UrlPathSegment { get; } = "settings";

    public ReactiveCommand<Unit, Unit> GoBack => MainViewModel.RouterInstance.NavigateBack;

    private StreamViewModel _streamViewModel;

    public StreamState state { get; set; }

    public ProgressViewModel progress { get; set; }

    private List<SettingViewModel> _settings;
    public List<SettingViewModel> Settings
    {
      get => _settings;
      private set => this.RaiseAndSetIfChanged(ref _settings, value);
    }

    public SettingsPageViewModel(IScreen screen, List<SettingViewModel> settings, StreamViewModel streamViewModel)
    {
      HostScreen = screen;
      Settings = settings;
      _streamViewModel = streamViewModel;
    }

    public SettingsPageViewModel(IScreen screen, List<SettingViewModel> settings, StreamViewModel streamViewModel, StreamState localState, ProgressViewModel localProgress)
    {
      HostScreen = screen;
      Settings = settings;
      _streamViewModel = streamViewModel;
      state = localState;
      progress = localProgress;
    }

    public async void MappingCommand()
    {
      var kit = KitManager.GetDefaultKit();

     // var converter = kit.LoadConverter(ConnectorRevitUtils.RevitAppName);
     // converter.SetContextDocument(CurrentDoc.Document);
     // var previouslyReceiveObjects = state.ReceivedObjects;

     // set converter settings as tuples (setting slug, setting selection)
     //var settings = new Dictionary<string, string>();
     // CurrentSettings = state.Settings;
     // foreach (var setting in state.Settings)
     //   settings.Add(setting.Slug, setting.Selection);
     // converter.SetConverterSettings(settings);

      var transport = new ServerTransport(state.Client.Account, state.StreamId);

      var stream = await state.Client.StreamGet(state.StreamId);

      if (progress.CancellationTokenSource.Token.IsCancellationRequested)
      {
        return;
      }

      Commit myCommit = null;
      //if "latest", always make sure we get the latest commit when the user clicks "receive"
      if (state.CommitId == "latest")
      {
        var res = await state.Client.BranchGet(progress.CancellationTokenSource.Token, state.StreamId, state.BranchName, 1);
        myCommit = res.commits.items.FirstOrDefault();
      }
      else
      {
        myCommit = await state.Client.CommitGet(progress.CancellationTokenSource.Token, state.StreamId, state.CommitId);
      }
      string referencedObject = myCommit.referencedObject;

      var commitObject = await Operations.Receive(
          referencedObject,
          progress.CancellationTokenSource.Token,
          transport,
          onProgressAction: dict => progress.Update(dict),
          onErrorAction: (s, e) =>
          {
            progress.Report.LogOperationError(e);
            progress.CancellationTokenSource.Cancel();
          },
          onTotalChildrenCountKnown: count => { progress.Max = count; },
          disposeTransports: true
          );

      if (progress.CancellationTokenSource.Token.IsCancellationRequested)
      {
        return;
      }

      var vm = new MappingViewModel();
      var mappingView = new MappingView
      {
        DataContext = vm
      };

      mappingView.Show();
    }

    public void SaveCommand()
    {
      _streamViewModel.Settings = Settings.Select(x => x.Setting).ToList();

      MainViewModel.RouterInstance.NavigateBack.Execute();
    }

  }
}
