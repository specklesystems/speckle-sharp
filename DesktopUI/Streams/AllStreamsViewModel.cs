using System;
using System.Collections.ObjectModel;
using System.Windows;
using MaterialDesignThemes.Wpf;
using Speckle.Core.Api;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.DesktopUI.Streams
{
  public class AllStreamsViewModel : Screen, IHandle<StreamAddedEvent>
  {
    private readonly IViewManager _viewManager;
    private IWindowManager _windowManager;
    private IStreamViewModelFactory _streamViewModelFactory;
    private IDialogFactory _dialogFactory;
    private IEventAggregator _events;
    public AllStreamsViewModel(
      IViewManager viewManager,
      IWindowManager windowManager,
      IStreamViewModelFactory streamViewModelFactory,
      IDialogFactory dialogFactory,
      IEventAggregator events)
    {
      _repo = new StreamsRepository();
      _events = events;
      DisplayName = "Home";
      _viewManager = viewManager;
      _windowManager = windowManager;
      _streamViewModelFactory = streamViewModelFactory;
      _dialogFactory = dialogFactory;

#if DEBUG
      _allStreams = _repo.LoadTestStreams();
#else
      // do this properly
#endif
      events.Subscribe(this);
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

    public async void ShowStreamCreateDialog()
    {
      var viewmodel = _dialogFactory.CreateStreamCreateDialog();
      var view = _viewManager.CreateAndBindViewForModelIfNecessary(viewmodel);

      var result = await DialogHost.Show(view, dialogIdentifier: "AllStreamsDialogHost");

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

    public void Handle(StreamAddedEvent message)
    {
      AllStreams.Insert(0, message.NewStream);
    }
  }
}