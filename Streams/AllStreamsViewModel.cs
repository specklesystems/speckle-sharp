using System;
using System.Collections.ObjectModel;
using System.Windows;
using Speckle.Core.Api;
using MaterialDesignThemes.Wpf;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.DesktopUI.Streams
{
  public class AllStreamsViewModel : Screen
  {
    private IWindowManager _windowManager;
    private IStreamViewModelFactory _streamViewModelFactory;
    private IDialogFactory _dialogFactory;
    private IEventAggregator _events;
    public AllStreamsViewModel(
      IWindowManager windowManager,
      IStreamViewModelFactory streamViewModelFactory,
      IDialogFactory dialogFactory,
      IEventAggregator events)
    {
      _repo = new StreamsRepository();
      _events = events;
      DisplayName = "Home";
      _windowManager = windowManager;
      _streamViewModelFactory = streamViewModelFactory;
      _dialogFactory = dialogFactory;

#if DEBUG
      _allStreams = _repo.LoadTestStreams();
#else
      // do this properly
#endif
    }

    private StreamsRepository _repo;
    private ObservableCollection<Stream> _allStreams;
    private Stream _selectedStream;
    private Branch _selectedBranch;

    public void ShowStreamInfo(Stream stream)
    {
      var item = _streamViewModelFactory.CreateStreamViewModel();
      item.Stream = stream;
      // get master branch for now
      // TODO allow user to select branch
      item.Branch = _repo.GetMasterBranch(stream.branches.items);
      var parent = (StreamsHomeViewModel)Parent;
      parent.ActivateItem(item);
    }
    public ObservableCollection<Stream> AllStreams
    {
      get => _allStreams;
      set => SetAndNotify(ref _allStreams, value);
    }

    public Stream SelectedStream
    {
      get => _selectedStream;
      set => SetAndNotify(ref _selectedStream, value);
    }

    public Branch SelectedBranch
    {
      get => _selectedBranch;
      set => SetAndNotify(ref _selectedBranch, value);
    }

    public StreamCreateDialogViewModel StreamCreateDialog
    {
      get => _dialogFactory.CreateStreamCreateDialog();
    }

    public async void ShowStreamCreateDialog()
    {
      var view = new StreamCreateDialogView()
      {
        DataContext = _dialogFactory.CreateStreamCreateDialog()
      };

      var result = await DialogHost.Show(view, dialogIdentifier: "AllStreamsDialogHost");
      //var result = _windowManager.ShowDialog(dialogvm);
    }

    public void CopyStreamId(string streamId)
    {
      Clipboard.SetText(streamId);
      _events.Publish(new ShowNotificationEvent()
      {
        Notification = "Stream ID copied to clipboard!"
      });
    }

    public void OpenHelpLink(string url)
    {
      Link.OpenInBrowser(url);
    }
  }
}
