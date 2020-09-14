using System;
using System.Collections.ObjectModel;
using System.Windows;
using Speckle.Core.Api;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.DesktopUI.Streams
{
  public class AllStreamsViewModel : Screen
  {
    private IStreamViewModelFactory _streamViewModelFactory;
    private IEventAggregator _events;
    public AllStreamsViewModel(
      IStreamViewModelFactory streamViewModelFactory,
      IEventAggregator events)
    {
      _repo = new StreamsRepository();
      _events = events;
      DisplayName = "Home";
      SelectedSlide = 0;
      _streamViewModelFactory = streamViewModelFactory;

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
    private int _selectedSlide;

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

    public int SelectedSlide
    {
      get => _selectedSlide;
      set => SetAndNotify(ref _selectedSlide, value);
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

  public interface IStreamViewModelFactory
  {
    StreamViewModel CreateStreamViewModel();
  }
}