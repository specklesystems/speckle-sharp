using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.DesktopUI.Streams
{
  public class StreamUpdateDialogViewModel : Conductor<IScreen>
  {
    private readonly IEventAggregator _events;
    private readonly ConnectorBindings _bindings;

    public StreamUpdateDialogViewModel(
      IEventAggregator events,
      ConnectorBindings bindings)
    {
      DisplayName = "Update Stream";
      _events = events;
      _bindings = bindings;
      _filters = new BindableCollection<ISelectionFilter>(_bindings.GetSelectionFilters());
    }

    private StreamState _streamState;
    public StreamState StreamState
    {
      get => _streamState;
      set => SetAndNotify(ref _streamState, value);
    }

    private int _selectedSlide = 0;
    public int SelectedSlide
    {
      get => _selectedSlide;
      set => SetAndNotify(ref _selectedSlide, value);
    }

    private BindableCollection<ISelectionFilter> _filters;

    public BindableCollection<ISelectionFilter> Filters
    {
      get => new BindableCollection<ISelectionFilter>(_filters);
      set => SetAndNotify(ref _filters, value);
    }

    private ISelectionFilter _selectedFilter;

    public ISelectionFilter SelectedFilter
    {
      get => _selectedFilter;
      set
      {
        SetAndNotify(ref _selectedFilter, value);
        NotifyOfPropertyChange(nameof(CanGetSelectedObjects));
      }
    }

    public bool CanGetSelectedObjects
    {
      get => SelectedFilter != null;
    }

    // TODO extract dialog logic into separate manager to better handle open / close
    public void CloseDialog()
    {
      DialogHost.CloseDialogCommand.Execute(null, null);
    }
  }
}
