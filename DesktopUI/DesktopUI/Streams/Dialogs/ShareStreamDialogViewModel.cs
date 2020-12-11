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

    private BindableCollection<User> _selectedUsers = new BindableCollection<User>();

    public BindableCollection<User> SelectedUsers
    {
      get => _selectedUsers;
      set => SetAndNotify(ref _selectedUsers, value);
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

    public void SelectAllText(TextBox sender, EventArgs args)
    {
      sender.SelectAll();
    }

    private bool _dropdownState = false;

    public bool DropdownState
    {
      get => _dropdownState;
      set => SetAndNotify(ref _dropdownState, value);
    }

    private bool sourceChanged = false;

    public async void SearchForUsers()
    {
      if (UserQuery.Length <= 2)
        return;

      try
      {
        var users = await StreamState.Client.UserSearch(UserQuery);
        DropdownState = sourceChanged = true;
        UserSearchResults = new BindableCollection<User>(users);
      }
      catch (Exception e)
      {
        // search prob returned no results
        UserSearchResults?.Clear();
      }
    }

    private bool _canAddCollaborators;

    public bool CanAddCollaborators
    {
      get => _canAddCollaborators;
      set => SetAndNotify(ref _canAddCollaborators, value);
    }

    public async void AddCollaborators()
    {
      var errors = new BindableCollection<User>();
      foreach (var user in SelectedUsers)
      {
        try
        {
          var res = await StreamState.Client.StreamGrantPermission(new StreamGrantPermissionInput()
          {
            streamId = StreamState.Stream.id,
            role = SelectedRole.Role,
            userId = user.id
          });
          if (!res)
            errors.Add(user);
        }
        catch (Exception)
        {
          errors.Add(user);
        }
      }

      _events.Publish(new StreamUpdatedEvent(StreamState.Stream));

      if (errors.Count != 0)
      {
        SelectedUsers = errors;
        var message =
          $"Could not add {errors.Count} {SelectedRole.Name.ToLower()}{Formatting.PluralS(errors.Count)} to stream:\n";
        message = errors.Aggregate(message, (current, user) => current + $"{user.name}, ");

        message = message.Remove(message.Length - 2);
        Notifications.Enqueue(message);
        return;
      }

      _bindings.RaiseNotification(
        $"Added {SelectedUsers.Count} {SelectedRole.Name.ToLower()}{Formatting.PluralS(errors.Count)} to this stream");
      CloseDialog();
    }

    public void ToggleDropdown()
    {
      DropdownState = !DropdownState;
    }

    public void UserSelectionChanged(ListBox sender, SelectionChangedEventArgs e)
    {
      // we're only allowing adding items by click,
      //so if changed items is more than 1, something is sus
      if (e.AddedItems.Count == 1)
      {
        var added = (User)e.AddedItems[0];
        if (!SelectedUsers.Any(s => s.id == added.id))
          SelectedUsers.Add((User)e.AddedItems[0]);
        CanAddCollaborators = true;
        return;
      }

      // avoid removing all selected users when new search is made
      if (sourceChanged)
      {
        sourceChanged = !sourceChanged;

        var selected = sender.Items.Cast<User>().Where(sel => SelectedUsers.Any(u => sel.id == u.id));
        foreach (var s in selected)
        {
          sender.SelectedItems.Add(s);
        }

        return;
      }

      if (e.RemovedItems.Count == 1)
      {
        var removed = (User)e.RemovedItems[0];
        var toRemove = SelectedUsers.FirstOrDefault(s => s.id == removed.id);
        SelectedUsers.Remove(toRemove);
        CanAddCollaborators = SelectedUsers.Count != 0;
      }
    }

    public async void ToggleShareLink()
    {
      ShareLinkVisible = !ShareLinkVisible; // toggle sharing

      if (ShareLinkVisible != StreamState.Stream.isPublic)
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
        catch (Exception e)
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

    // TODO extract dialog logic into separate manager to better handle open / close
    public void CloseDialog()
    {
      DialogHost.CloseDialogCommand.Execute(null, null);
    }
  }
}
