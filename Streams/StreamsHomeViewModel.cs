using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf.Transitions;
using Speckle.Core.Api;
using Speckle.DesktopUI.Utils;

namespace Speckle.DesktopUI.Streams
{
  class StreamsHomeViewModel : BindableBase
  {
    public StreamsHomeViewModel()
    {
      _repo = new StreamsRepository();
      SelectedSlide = 0;

#if DEBUG
      _allStreams = _repo.LoadTestStreams();
#else
      // do this properly
#endif
      ShowStreamInfoCommand = new RelayCommand<Stream>(OnShowStreamInfo);
    }

    private StreamsRepository _repo;
    private ConnectorBindings _connectorBindings;

    private ObservableCollection<Stream> _allStreams;
    private Stream _selectedStream;
    private Branch _selectedBranch;
    private int _selectedSlide;

    public ConnectorBindings ConnectorBindings
    {
      get => _connectorBindings;
      set => SetProperty(ref _connectorBindings, value);
    }
    public RelayCommand<Stream> ShowStreamInfoCommand { get; set; }
    private void OnShowStreamInfo(Stream stream)
    {
      SelectedStream = stream;
      SelectedBranch = _repo.GetMasterBranch(stream.branches.items);
      SelectedSlide += 1; //TODO: this is sloppy lol fix this
    }
    public ObservableCollection<Stream> AllStreams
    {
      get => _allStreams;
      set => SetProperty(ref _allStreams, value);
    }

    public Stream SelectedStream
    {
      get => _selectedStream;
      set => SetProperty(ref _selectedStream, value);
    }

    public Branch SelectedBranch
    {
      get => _selectedBranch;
      set => SetProperty(ref _selectedBranch, value);
    }

    public int SelectedSlide
    {
      get => _selectedSlide;
      set => SetProperty(ref _selectedSlide, value);
    }
  }
}