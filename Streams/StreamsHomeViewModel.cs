using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf.Transitions;
using Speckle.Core.Api;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.DesktopUI.Streams
{
  class StreamsHomeViewModel : Screen
  {
    public StreamsHomeViewModel()
    {
      _repo = new StreamsRepository();
      DisplayName = "Home";
      SelectedSlide = 0;

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
      SelectedStream = stream;
      SelectedBranch = _repo.GetMasterBranch(stream.branches.items);
      SelectedSlide += 1; //TODO: this is sloppy lol fix this
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
  }
}
