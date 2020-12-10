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

    private User _selectedUser;

    public User SelectedUser
    {
      get => _selectedUser;
      set => SetAndNotify(ref _selectedUser, value);
    }

    private string _shareMessage;

    public string ShareMessage
    {
      get => _shareMessage;
      set => SetAndNotify(ref _shareMessage, value);
    }

    private bool _shareLink;

    public bool ShareLink
    {
      get => _shareLink;
      set => SetAndNotify(ref _shareLink, value);
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
      if ( UserQuery.Length <= 2 )
        return;

      try
      {
        var users = await StreamState.Client.UserSearch(UserQuery);
        DropdownState = sourceChanged = true;
        UserSearchResults = new BindableCollection<User>(users);
      }
      catch ( Exception e )
      {
        // search prob returned no results
        UserSearchResults?.Clear();
      }
    }

    public void ToggleDropdown()
    {
      DropdownState = !DropdownState;
    }

    public void UserSelectionChanged(ListBox sender, SelectionChangedEventArgs e)
    {
      // we're only allowing adding items by click, 
      //so if changed items is more than 1, something is sus
      if ( e.AddedItems.Count == 1 )
      {
        var added = ( User ) e.AddedItems[ 0 ];
        if ( !SelectedUsers.Any(s => s.id == added.id) )
          SelectedUsers.Add(( User ) e.AddedItems[ 0 ]);
      }

      // avoid removing all selected users when new search is made
      if ( sourceChanged )
      {
        sourceChanged = !sourceChanged;

        var selected = sender.Items.Cast<User>().Where(sel => SelectedUsers.Any(u => sel.id == u.id));
        foreach ( var s in selected )
        {
          sender.SelectedItems.Add(s);
        }

        return;
      }

      if ( e.RemovedItems.Count == 1 )
      {
        var removed = ( User ) e.RemovedItems[ 0 ];
        var toRemove = SelectedUsers.FirstOrDefault(s => s.id == removed.id);
        SelectedUsers.Remove(toRemove);
      }
    }

    // TODO extract dialog logic into separate manager to better handle open / close
    public void CloseDialog()
    {
      DialogHost.CloseDialogCommand.Execute(null, null);
    }
  }
}
