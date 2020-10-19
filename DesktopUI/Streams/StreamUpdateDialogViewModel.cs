using System;
using MaterialDesignThemes.Wpf;
using Speckle.Core.Api;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.DesktopUI.Streams
{
  public class StreamUpdateDialogViewModel : Conductor<IScreen>
  {
    private readonly IEventAggregator _events;
    private readonly ConnectorBindings _bindings;
    private ISnackbarMessageQueue _notifications = new SnackbarMessageQueue(TimeSpan.FromSeconds(5));

    public StreamUpdateDialogViewModel(
      IEventAggregator events,
      ConnectorBindings bindings)
    {
      DisplayName = "Update Stream";
      _events = events;
      _bindings = bindings;
      _filters = new BindableCollection<ISelectionFilter>(_bindings.GetSelectionFilters());
    }

    public ISnackbarMessageQueue Notifications
    {
      get => _notifications;
      set => SetAndNotify(ref _notifications, value);
    }

    private StreamState _streamState;
    public StreamState StreamState
    {
      get => _streamState;
      set
      {
        SetAndNotify(ref _streamState, value);
        NewName = StreamState.stream.name;
        NewDescription = StreamState.stream.description;
      }
    }

    private string _newName;

    public string NewName
    {
      get => _newName;
      set => SetAndNotify(ref _newName, value);
    }

    private string _newDescription;

    public string NewDescription
    {
      get => _newDescription;
      set => SetAndNotify(ref _newDescription, value);
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

    public async void UpdateStream()
    {
      if ( NewName == StreamState.stream.name && NewDescription == StreamState.stream.description ) CloseDialog();
      try
      {
        var res = await StreamState.client.StreamUpdate(new StreamUpdateInput()
        {
          id = StreamState.stream.id,
          name = NewName,
          description = NewDescription,
          isPublic = StreamState.stream.isPublic
        });
        _events.Publish(new StreamUpdatedEvent()
        {
          StreamId = StreamState.stream.id
        });
        CloseDialog();
      }
      catch ( Exception e )
      {
        Notifications.Enqueue($"Error: {e}");
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
