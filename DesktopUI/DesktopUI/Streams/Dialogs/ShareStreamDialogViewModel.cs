using System;
using System.Collections.Generic;
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
    private readonly StreamsRepository _streamsRepo;
    private readonly ConnectorBindings _bindings;

    public ShareStreamDialogViewModel(
      IEventAggregator events,
      StreamsRepository streamsRepo,
      ConnectorBindings bindings)
    {
      DisplayName = "Share Stream";
      _events = events;
      _streamsRepo = streamsRepo;
      _bindings = bindings;

      Roles = _streamsRepo.GetRoles();
      SelectedRole = Roles[0];

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

    private StreamRole _selectedRole;

    public StreamRole SelectedRole
    {
      get => _selectedRole;
      set => SetAndNotify(ref _selectedRole, value);
    }

    public string ShareLink => $"{StreamState.ServerUrl}/streams/{StreamState.Stream.id}";


    //public bool IsPublic
    //{
    //  get => StreamState.Stream.isPublic;
    //  //set => SetAndNotify(ref _isPublic, value);
    //}

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
      if (UserQuery.Length <= 2)
        return;

      try
      {
        var users = await StreamState.Client.UserSearch(UserQuery);
        DropdownState = true; // open search dropdown when there are results
        UserSearchResults = new BindableCollection<User>(users);
      }
      catch (Exception e)
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
          streamId = StreamState.Stream.id,
          role = SelectedRole.Role,
          userId = SelectedUser.id
        });
      }
      catch (Exception e)
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
      if (e.AddedItems.Count == 1)
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
    public async void ToggleIsPublic()
    {
      //IsPublic = !IsPublic;

      //if (IsPublic != StreamState.Stream.isPublic)
      //{
      try
      {
        await StreamState.Client.StreamUpdate(new StreamUpdateInput()
        {
          id = StreamState.Stream.id,
          name = StreamState.Stream.name,
          description = StreamState.Stream.description,
          isPublic = !StreamState.Stream.isPublic
        });
        _events.Publish(new StreamUpdatedEvent(StreamState.Stream));
      }
      catch (Exception e)
      {
        Notifications.Enqueue($"Could not set to {(!StreamState.Stream.isPublic ? "public" : "private")}. Error: {e.Message}");
      }
      //}
    }

    public void OpenEmailInviteLink()
    {
      Link.OpenInBrowser($"{ShareLink}?invite=true");
    }

    public List<StreamRole> Roles { get; set; }

    public void CloseDialog()
    {
      DialogHost.CloseDialogCommand.Execute(null, null);
    }
  }
}
