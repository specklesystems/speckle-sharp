using System;
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
      Roles = new BindableCollection<StreamRole>(_streamsRepo.GetRoles());
      FilterTabs = new BindableCollection<FilterTab>(Bindings.GetSelectionFilters().Select(f => new FilterTab(f)));

      _events.Subscribe(this);
    }

    public bool EditingDetails
    {
      get => SelectedSlide == 0;
    }

    public bool EditingObjects
    {
      get => SelectedSlide == 1;
    }

    public bool EditingCollabs
    {
      get => SelectedSlide == 2;
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
        var update = await StreamState.Client.StreamGet(StreamState.Stream.id);
        _events.Publish(new StreamUpdatedEvent(update));
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
      _events.Publish(new StreamUpdatedEvent(StreamState.Stream));
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

    public async void AddCollaboratorsToStream()
    {
      if ( Role == null )
      {
        Notifications.Enqueue("Please select a role");
        return;
      }
      if ( !Collaborators.Any() ) return;
      var success = 0;
      foreach ( var collaborator in Collaborators )
      {
        try
        {
          var res = await StreamState.Client.StreamGrantPermission(new StreamGrantPermissionInput()
          {
            role = Role.Role, streamId = StreamState.Stream.id, userId = collaborator.id
          });
          if ( res ) success++;
        }
        catch ( Exception e )
        {
          Notifications.Enqueue($"Failed to add collaborators: {e}");
          return;
        }
      }

      if ( success == 0 )
      {
        Notifications.Enqueue("Could not add collaborators to this stream");
        return;
      }

      StreamState.Stream = await StreamState.Client.StreamGet(StreamState.Stream.id);
      Collaborators.Clear();
      _events.Publish(new StreamUpdatedEvent(StreamState.Stream));
      Notifications.Enqueue($"Added {success} collaborators to this stream");
    }

    public async void RemoveCollaborator(Collaborator collaborator)
    {
      try
      {
        var res = await StreamState.Client.StreamRevokePermission(new StreamRevokePermissionInput()
        {
          streamId = StreamState.Stream.id, userId = collaborator.id
        });
        if ( !res )
        {
          Notifications.Enqueue($"Could not revoke {collaborator.name}'s permissions");
          return;
        }
      }
      catch ( Exception e )
      {
        Notifications.Enqueue($"Could not revoke {collaborator.name}'s permissions: {e}");
        return;
      }

      StreamState.Stream = await StreamState.Client.StreamGet(StreamState.Stream.id);
      _events.Publish(new StreamUpdatedEvent(StreamState.Stream));
      Notifications.Enqueue($"Revoked {collaborator.name}'s permissions");
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
