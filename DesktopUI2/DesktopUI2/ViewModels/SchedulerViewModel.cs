using DesktopUI2.Models;
using ReactiveUI;
using Speckle.Core.Logging;
using System.Collections.ObjectModel;

namespace DesktopUI2.ViewModels
{
  public class SchedulerViewModel : ReactiveObject
  {

    public ConnectorBindings Bindings { get; private set; } = new DummyBindings();

    private ObservableCollection<StreamState> _savedStreams = new ObservableCollection<StreamState>();
    public ObservableCollection<StreamState> SavedStreams
    {
      get => _savedStreams;
      set
      {
        this.RaiseAndSetIfChanged(ref _savedStreams, value);
      }
    }

    public string Title => "for " + Bindings.GetHostAppNameVersion();
    public string TitleFull => "Speckle for " + Bindings.GetHostAppNameVersion();
    public SchedulerViewModel(ConnectorBindings _bindings)
    {
      Bindings = _bindings;
      Setup.Init(Bindings.GetHostAppNameVersion(), Bindings.GetHostAppName());
      Init();

    }
    public SchedulerViewModel()
    {
      Init();
    }

    public void Init()
    {
      SavedStreams = new ObservableCollection<StreamState>(Bindings.GetStreamsInFile());
    }






  }
}
