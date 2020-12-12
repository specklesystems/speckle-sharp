using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
using Speckle.Core.Api;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.DesktopUI.Streams
{
  public class ShareStreamDialogViewModel : Conductor<IScreen>
  {
    private readonly IEventAggregator _events;
    private readonly ConnectorBindings _bindings;

    public ShareStreamDialogViewModel(
      IEventAggregator events,
      ConnectorBindings bindings)
    {
      DisplayName = "Share Stream";
      _events = events;
      _bindings = bindings;

      Roles = new List<CollabRole>()
      {
        new CollabRole("Contributor", "stream:contributor", "Can edit, push and pull."),
        new CollabRole("Reviewer", "stream:reviewer", "Can only view."),
        new CollabRole("Owner", "stream:owner", "Has full access, including deletion rights & access control.")
      };
      SelectedRole = Roles[ 0 ];
    }

    private ISnackbarMessageQueue _notifications = new SnackbarMessageQueue(TimeSpan.FromSeconds(5));

    public ISnackbarMessageQueue Notifications
    {
      get => _notifications;
      set => SetAndNotify(ref _notifications, value);
    }

    private StreamState _streamState;

    public StreamState StreamState
    {
      get => _streamState;
      set => SetAndNotify(ref _streamState, value);
    }

    private string _userQuery;

    public string UserQuery
    {
      get => _userQuery;
      set
      {
        SetAndNotify(ref _userQuery, value);
        SearchForUsers();
      }
    }

    private BindableCollection<User> _userSearchResults;

    public BindableCollection<User> UserSearchResults
    {
      get => _userSearchResults;
      set => SetAndNotify(ref _userSearchResults, value);
    }

    private User _selectedUser;

    public User SelectedUser
    {
      get => _selectedUser;
      set => SetAndNotify(ref _selectedUser, value);
    }

    private CollabRole _selectedRole;

    public CollabRole SelectedRole
    {
      get => _selectedRole;
      set => SetAndNotify(ref _selectedRole, value);
    }

    public string ShareLink => $"{StreamState.ServerUrl}/streams/{StreamState.Stream.id}";

    private bool _shareLinkVisible;

    public bool ShareLinkVisible
    {
      get => _shareLinkVisible;
      set => SetAndNotify(ref _shareLinkVisible, value);
    }

    // select full share link in link sharing box on click 
    public void SelectAllText(TextBox sender, EventArgs args)
    {
      sender.SelectAll();
    }

    private bool _dropdownState = false;

    public bool DropdownState
    {
      get => _dropdownState;
      set { SetAndNotify(ref _dropdownState, value); }
    }

    public async void SearchForUsers()
    {
      if ( UserQuery.Length <= 2 )
        return;

      try
      {
        var users = await StreamState.Client.UserSearch(UserQuery);
        DropdownState = true; // open search dropdown when there are results
        UserSearchResults = new BindableCollection<User>(users);
      }
      catch ( Exception e )
      {
        // search prob returned no results
        UserSearchResults?.Clear();
      }
    }

    public async void AddCollaborator()
    {
      try
      {
        var res = await StreamState.Client.StreamGrantPermission(new StreamGrantPermissionInput()
        {
          streamId = StreamState.Stream.id, role = SelectedRole.Role, userId = SelectedUser.id
        });
      }
      catch ( Exception e )
      {
        Notifications.Enqueue($"Sorry - could not add {SelectedUser.name} to this stream. Error: {e.Message}");
        return;
      }

      _events.Publish(new StreamUpdatedEvent(StreamState.Stream));
      Notifications.Enqueue(
        $"Added {SelectedUser.name} as a {SelectedRole.Name.ToLower()} to this stream");
      ClearSelection();
    }

    // toggle search results dropdown
    public void ToggleDropdown()
    {
      DropdownState = !DropdownState;
    }

    // close the dropdown when a user is selected
    public void UserSelected(ListBox sender, SelectionChangedEventArgs e)
    {
      if ( e.AddedItems.Count == 1 )
      {
        DropdownState = false;
      }
    }

    public void ClearSelection()
    {
      SelectedUser = null;
      UserQuery = "";
    }

    // turn on or off link sharing of the stream 
    // doesn't work right now - server bug doesn't allow flipping `isPublic`
    public async void ToggleShareLink()
    {
      ShareLinkVisible = !ShareLinkVisible;

      if ( ShareLinkVisible != StreamState.Stream.isPublic )
      {
        try
        {
          await StreamState.Client.StreamUpdate(new StreamUpdateInput()
          {
            id = StreamState.Stream.id,
            name = StreamState.Stream.name,
            description = StreamState.Stream.description,
            isPublic = ShareLinkVisible
          });
          _events.Publish(new StreamUpdatedEvent(StreamState.Stream));
        }
        catch ( Exception e )
        {
          Notifications.Enqueue($"Could not set link sharing to {ShareLinkVisible}. Error: {e.Message}");
        }
      }
    }

    public List<CollabRole> Roles { get; set; }

    public class CollabRole
    {
      public CollabRole(string name, string role, string description = "")
      {
        Name = name;
        Role = role;
        Description = description;
      }

      public string Name { get; set; }
      public string Role { get; set; }
      public string Description { get; set; }
    }

    public void CloseDialog()
    {
      DialogHost.CloseDialogCommand.Execute(null, null);
    }
  }
}
