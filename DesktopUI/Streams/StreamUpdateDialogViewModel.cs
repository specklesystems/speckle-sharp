using System;
using System.Collections.Generic;
using System.Linq;
using MaterialDesignThemes.Wpf;
using Speckle.Core.Api;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.DesktopUI.Streams
{
  public class StreamUpdateDialogViewModel : Conductor<IScreen>.Collection.OneActive,
    IHandle<RetrievedFilteredObjectsEvent>, IHandle<UpdateSelectionCountEvent>
  {
    private readonly IEventAggregator _events;
    private readonly ConnectorBindings _bindings;
    private ISnackbarMessageQueue _notifications = new SnackbarMessageQueue(TimeSpan.FromSeconds(5));

    public StreamUpdateDialogViewModel(
      IEventAggregator events,
      StreamsRepository streamsRepo,
      ConnectorBindings bindings)
    {
      DisplayName = "Update Stream";
      _events = events;
      _streamsRepo = streamsRepo;
      _bindings = bindings;
      _filters = new BindableCollection<ISelectionFilter>(_bindings.GetSelectionFilters());

      _events.Subscribe(this);
    }

    private readonly StreamsRepository _streamsRepo;

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
        NewName = StreamState.Stream.name;
        NewDescription = StreamState.Stream.description;
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

    private int _selectedSlide;

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

    public string ActiveViewName
    {
      get => _bindings.GetActiveViewName();
    }

    public List<string> ActiveViewObjects
    {
      get => _bindings.GetObjectsInView();
    }

    public List<string> CurrentSelection
    {
      get => _bindings.GetSelectedObjects();
    }

    private int _selectionCount;

    public int SelectionCount
    {
      get => _selectionCount;
      set => SetAndNotify(ref _selectionCount, value);
    }

    private bool _updateButtonLoading;

    public bool UpdateButtonLoading
    {
      get => _updateButtonLoading;
      set => SetAndNotify(ref _updateButtonLoading, value);
    }

    public async void UpdateStreamDetails()
    {
      if ( NewName == StreamState.Stream.name && NewDescription == StreamState.Stream.description ) CloseDialog();
      try
      {
        var res = await StreamState.Client.StreamUpdate(new StreamUpdateInput()
        {
          id = StreamState.Stream.id,
          name = NewName,
          description = NewDescription,
          isPublic = StreamState.Stream.isPublic
        });
        _events.Publish(new StreamUpdatedEvent() {StreamId = StreamState.Stream.id});
        CloseDialog();
      }
      catch ( Exception e )
      {
        Notifications.Enqueue($"Error: {e}");
      }
    }

    public async void UpdateStreamObjects()
    {
      UpdateButtonLoading = true;
      StreamState.Filter = SelectedFilter;
      _bindings.UpdateStream(StreamState);
      _events.Publish(new StreamUpdatedEvent());
      UpdateButtonLoading = false;
      CloseDialog();
    }

    public void UpdateFromSelection()
    {
      UpdateButtonLoading = true;
      SelectedFilter = Filters.First(filter => filter.Type == typeof(ElementsSelectionFilter).ToString());
      GetSelectedObjects();

      UpdateStreamObjects();
    }

    public void UpdateFromView()
    {
      SelectedFilter = Filters.First(filter => filter.Type == typeof(ElementsSelectionFilter).ToString());
      SelectedFilter.Selection = ActiveViewObjects;

      UpdateStreamObjects();
    }

    public bool CanGetSelectedObjects
    {
      get => SelectedFilter != null;
    }

    public void GetSelectedObjects()
    {
      if ( SelectedFilter == null )
      {
        Notifications.Enqueue("pls click one of the filter types!");
        return;
      }

      if ( SelectedFilter.Type == typeof(ElementsSelectionFilter).ToString() )
      {
        var selectedObjs = _bindings.GetSelectedObjects();
        SelectedFilter.Selection = selectedObjs;
        NotifyOfPropertyChange(nameof(SelectedFilter.Selection.Count));
      }
      else
      {
        Notifications.Enqueue("soz this only works for selection!");
      }
    }


    // TODO extract dialog logic into separate manager to better handle open / close
    public void CloseDialog()
    {
      DialogHost.CloseDialogCommand.Execute(null, null);
    }

    public void Handle(RetrievedFilteredObjectsEvent message)
    {
      StreamState.Placeholders = message.Objects.ToList();
    }

    public void Handle(UpdateSelectionCountEvent message)
    {
      SelectionCount = message.SelectionCount;
    }
  }
}
