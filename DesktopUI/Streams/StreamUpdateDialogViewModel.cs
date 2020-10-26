using System;
using System.Collections.Generic;
using System.Linq;
using MaterialDesignThemes.Wpf;
using Speckle.Core.Api;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.DesktopUI.Streams
{
  public class StreamUpdateDialogViewModel : StreamDialogBase,
    IHandle<RetrievedFilteredObjectsEvent>, IHandle<UpdateSelectionCountEvent>
  {
    private readonly IEventAggregator _events;
    private ISnackbarMessageQueue _notifications = new SnackbarMessageQueue(TimeSpan.FromSeconds(5));

    public StreamUpdateDialogViewModel(
      IEventAggregator events,
      StreamsRepository streamsRepo,
      ConnectorBindings bindings)
    {
      DisplayName = "Update Stream";
      _events = events;
      _streamsRepo = streamsRepo;
      Bindings = bindings;

      FilterTabs = new BindableCollection<FilterTab>(Bindings.GetSelectionFilters().Select(f => new FilterTab(f)));

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
      var filter = SelectedFilterTab.Filter;
      switch ( filter.Name )
      {
        case "View":
        case "Category":
        case "Selection" when SelectedFilterTab.ListItems.Any():
          filter.Selection = SelectedFilterTab.ListItems.ToList();
          break;
      }

      StreamState.Filter = filter;
      Bindings.UpdateStream(StreamState);
      _events.Publish(new StreamUpdatedEvent());
      UpdateButtonLoading = false;
      CloseDialog();
    }

    public void UpdateFromSelection()
    {
      UpdateButtonLoading = true;
      SelectedFilterTab = FilterTabs.First(tab => tab.Filter.Name == "Selection");
      SelectedFilterTab.ListItems.Clear();
      SelectedFilterTab.Filter.Selection = Bindings.GetSelectedObjects();

      UpdateStreamObjects();
    }

    public void UpdateFromView()
    {
      SelectedFilterTab = FilterTabs.First(tab => tab.Filter.Name == "Selection");
      SelectedFilterTab.Filter.Selection = ActiveViewObjects;

      UpdateStreamObjects();
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
