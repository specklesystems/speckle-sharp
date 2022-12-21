﻿using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Metadata;
using Avalonia.Threading;
using DesktopUI2.Models;
using DesktopUI2.Views;
using DesktopUI2.Views.Controls;
using DesktopUI2.Views.Pages;
using DesktopUI2.Views.Windows.Dialogs;
using ReactiveUI;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Helpers;
using Speckle.Core.Logging;
using Splat;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;

namespace DesktopUI2.ViewModels
{
  public class CollaboratorsViewModel : ReactiveObject, IRoutableViewModel
  {
    public IScreen HostScreen { get; }
    public string UrlPathSegment { get; } = "collaborators";

    #region bindings

    public ReactiveCommand<Unit, Unit> GoBack => MainViewModel.RouterInstance.NavigateBack;

    private string _searchQuery = "";

    private Action userSearchDebouncer = null;

    public string SearchQuery
    {
      get => _searchQuery;
      set
      {
        this.RaiseAndSetIfChanged(ref _searchQuery, value);
        Search();
      }
    }
    private List<AccountViewModel> _users;
    public List<AccountViewModel> Users
    {
      get => _users;
      private set
      {
        this.RaiseAndSetIfChanged(ref _users, value);
      }
    }

    private ObservableCollection<AccountViewModel> _selectedUsers = new ObservableCollection<AccountViewModel>();
    public ObservableCollection<AccountViewModel> AddedUsers
    {
      get => _selectedUsers;
      private set
      {
        this.RaiseAndSetIfChanged(ref _selectedUsers, value);
      }
    }

    public AccountViewModel SelectedUser
    {
      set
      {
        if (value != null)
        {
          AddedUsers.Add(value);
          SearchQuery = "";
          this.RaisePropertyChanged(nameof(AddedUsers));
        }

      }
    }


    private bool _dropDownOpen;
    public bool DropDownOpen
    {
      get => _dropDownOpen;
      private set
      {
        this.RaiseAndSetIfChanged(ref _dropDownOpen, value);
      }
    }

    private bool _isDialog;
    public bool IsDialog
    {
      get => _isDialog;
      private set
      {
        this.RaiseAndSetIfChanged(ref _isDialog, value);
      }
    }

    private bool _showProgress;
    public bool ShowProgress
    {
      get => _showProgress;
      private set
      {
        this.RaiseAndSetIfChanged(ref _showProgress, value);
      }
    }

    private string _role;
    public string Role
    {
      get => _role;
      private set
      {
        this.RaiseAndSetIfChanged(ref _role, value);
      }
    }

    public bool HasSelectedUsers
    {
      get => SelectionModel.SelectedItems.Any();
    }

    public SelectionModel<AccountViewModel> SelectionModel { get; private set; }
    #endregion

    private StreamViewModel _stream;


    public CollaboratorsViewModel(IScreen screen, StreamViewModel stream)
    {
      HostScreen = screen;
      _stream = stream;
      Role = stream.Stream.role;

      userSearchDebouncer = Utils.Debounce(SearchUsers);

      SelectionModel = new SelectionModel<AccountViewModel>();
      SelectionModel.SingleSelect = false;
      SelectionModel.SelectionChanged += SelectionModel_SelectionChanged;

      IsDialog = MainViewModel.RouterInstance.NavigationStack.Last() is CollaboratorsViewModel;

      ReloadUsers();

    }

    internal void ReloadUsers()
    {
      AddedUsers = new ObservableCollection<AccountViewModel>();
      foreach (var collab in _stream.Stream.collaborators)
      {
        //skip myself
        //if (_stream.StreamState.Client.Account.userInfo.id == collab.id)
        //  continue;
        AddedUsers.Add(new AccountViewModel(collab));
      }

      foreach (var collab in _stream.Stream.pendingCollaborators)
      {
        AddedUsers.Add(new AccountViewModel(collab));
      }

      this.RaisePropertyChanged(nameof(AddedUsers));
    }

    private async void Search()
    {

      Focus();
      if (SearchQuery.Length < 3)
        return;

      if (!await Http.UserHasInternet())
      {
        Dispatcher.UIThread.Post(() =>
          MainUserControl.NotificationManager.Show(new PopUpNotificationViewModel()
          {
            Title = "⚠️ Oh no!",
            Message = "Could not reach the internet, are you connected?",
            Type = Avalonia.Controls.Notifications.NotificationType.Error
          }), DispatcherPriority.Background);

        return;
      }

      if (SearchQuery.Contains("@"))
      {
        if (Utils.IsValidEmail(SearchQuery))
        {
          var emailAcc = new AccountViewModel()
          {
            Name = SearchQuery
          };
          Users = new List<AccountViewModel> { emailAcc };

          ShowProgress = false;
          DropDownOpen = true;
        }
      }
      else
      {
        userSearchDebouncer();
      }

    }

    //focus is lost when the dropdown gets closed
    private void Focus()
    {
      DropDownOpen = false;
      var searchBox = CollaboratorsControl.Instance.FindControl<TextBox>("SearchBox");
      searchBox.Focus();
    }

    private void SelectionModel_SelectionChanged(object sender, SelectionModelSelectionChangedEventArgs<AccountViewModel> e)
    {
      this.RaisePropertyChanged("HasSelectedUsers");
    }

    private async void SearchUsers()
    {
      ShowProgress = true;

      //exclude existing ones
      var users = (await _stream.StreamState.Client.UserSearch(SearchQuery)).Where(x => !AddedUsers.Any(u => u.Id == x.id));
      //exclude myself
      users = users.Where(x => _stream.StreamState.Client.Account.userInfo.id != x.id);


      Users = users.Select(x => new AccountViewModel(x)).ToList();

      ShowProgress = false;
      DropDownOpen = true;

    }

    [DependsOn(nameof(AddedUsers))]
    bool CanSaveCommand(object parameter)
    {
      foreach (var user in AddedUsers)
      {
        if (Utils.IsValidEmail(user.Name) && !_stream.Stream.pendingCollaborators.Any(x => x.title == user.Name))
          return true;
        if (!_stream.Stream.collaborators.Any(x => x.id == user.Id) && !_stream.Stream.pendingCollaborators.Any(x => x.id == user.Id))
          return true;
        if (!_stream.Stream.collaborators.Any(x => x.id == user.Id && x.role == user.Role) && !_stream.Stream.pendingCollaborators.Any(x => x.id == user.Id && x.role == user.Role))
          return true;
      }
      foreach (var user in _stream.Stream.collaborators)
      {
        if (!AddedUsers.Any(x => x.Id == user.id))
          return true;
      }
      foreach (var user in _stream.Stream.pendingCollaborators)
      {
        if (!AddedUsers.Any(x => x.Id == user.id))
          return true;
      }
      return false;
    }

    async void SaveCommand()
    {

      if (!await Http.UserHasInternet())
      {
        Dispatcher.UIThread.Post(() =>
          MainUserControl.NotificationManager.Show(new PopUpNotificationViewModel()
          {
            Title = "⚠️ Oh no!",
            Message = "Could not reach the internet, are you connected?",
            Type = Avalonia.Controls.Notifications.NotificationType.Error
          }), DispatcherPriority.Background);

        return;
      }

      foreach (var user in AddedUsers)
      {
        //mismatch between roles set within the dropdown and existing ones
        if (!user.Role.StartsWith("stream:"))
          user.Role = "stream:" + user.Role;

        //invite users by email
        if (Utils.IsValidEmail(user.Name) && !_stream.Stream.pendingCollaborators.Any(x => x.title == user.Name))
        {
          try
          {
            await _stream.StreamState.Client.StreamInviteCreate(new StreamInviteCreateInput { email = user.Name, streamId = _stream.StreamState.StreamId, message = "I would like to share a model with you via Speckle!", role = user.Role });
            Analytics.TrackEvent(_stream.StreamState.Client.Account, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Share" }, { "method", "Invite Email" } });
          }
          catch (Exception e)
          {
            new SpeckleException("Error inviting user", e, true, Sentry.SentryLevel.Error);
          }
        }
        //add new collaborators
        else if (!_stream.Stream.collaborators.Any(x => x.id == user.Id) && !_stream.Stream.pendingCollaborators.Any(x => x.id == user.Id))
        {
          try
          {
            await _stream.StreamState.Client.StreamInviteCreate(new StreamInviteCreateInput { userId = user.Id, streamId = _stream.StreamState.StreamId, message = "I would like to share a model with you via Speckle!", role = user.Role });
            Analytics.TrackEvent(_stream.StreamState.Client.Account, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Share" }, { "method", "Invite User" } });
          }
          catch (Exception e)
          {
            new SpeckleException("Error adding collaborator", e, true, Sentry.SentryLevel.Error);
          }
        }
        //update permissions, only if changed
        else if (!_stream.Stream.collaborators.Any(x => x.id == user.Id && x.role == user.Role) && !_stream.Stream.pendingCollaborators.Any(x => x.id == user.Id && x.role == user.Role))
        {
          try
          {
            await _stream.StreamState.Client.StreamUpdatePermission(new StreamPermissionInput { userId = user.Id, streamId = _stream.StreamState.StreamId, role = user.Role });
            Analytics.TrackEvent(_stream.StreamState.Client.Account, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Share" }, { "method", "Update Permissions" } });
          }
          catch (Exception e)
          {
            new SpeckleException("Error updating permissions", e, true, Sentry.SentryLevel.Error);
          }
        }
      }

      //remove collaborators
      foreach (var user in _stream.Stream.collaborators)
      {
        if (!AddedUsers.Any(x => x.Id == user.id))
        {
          try
          {
            await _stream.StreamState.Client.StreamRevokePermission(new StreamRevokePermissionInput { userId = user.id, streamId = _stream.StreamState.StreamId });
            Analytics.TrackEvent(_stream.StreamState.Client.Account, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Share" }, { "method", "Remove User" } });
          }
          catch (Exception e)
          {
            new SpeckleException("Error updating permissions", e, true, Sentry.SentryLevel.Error);
          }
        }
      }

      //revoke invites
      foreach (var user in _stream.Stream.pendingCollaborators)
      {
        if (!AddedUsers.Any(x => x.Id == user.id))
        {
          try
          {
            await _stream.StreamState.Client.StreamInviteCancel(_stream.StreamState.StreamId, user.inviteId);
            Analytics.TrackEvent(_stream.StreamState.Client.Account, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Share" }, { "method", "Cancel Invite" } });
          }
          catch (Exception e)
          {
            new SpeckleException("Error updating permissions", e, true, Sentry.SentryLevel.Error);
          }
        }
      }

      try
      {
        _stream.Stream = await _stream.StreamState.Client.StreamGet(_stream.StreamState.StreamId);
        var pc = await _stream.StreamState.Client.StreamGetPendingCollaborators(_stream.StreamState.StreamId);
        _stream.Stream.pendingCollaborators = pc.pendingCollaborators;
        _stream.StreamState.CachedStream = _stream.Stream;

        ReloadUsers();
      }
      catch (Exception e)
      {
      }

      if (IsDialog)
        MainViewModel.RouterInstance.NavigateBack.Execute();

      this.RaisePropertyChanged(nameof(AddedUsers));
    }

    private async void CloseCommand()
    {
      MainViewModel.RouterInstance.NavigateBack.Execute();
    }


    private void ClearSearchCommand()
    {
      SearchQuery = "";
    }

    private void RemoveSeletedUsersCommand()
    {
      foreach (var item in SelectionModel.SelectedItems.ToList())
      {
        AddedUsers.Remove(item);
      }

      this.RaisePropertyChanged("HasSelectedUsers");
      this.RaisePropertyChanged(nameof(AddedUsers));

    }

    private async void ChangeRoleSeletedUsersCommand()
    {
      var dialog = new ChangeRoleDialog();
      var result = await dialog.ShowDialog<string>();

      if (result != null)
      {
        foreach (var item in SelectionModel.SelectedItems.ToList())
        {
          item.Role = "stream:" + result;
        }
      }

      this.RaisePropertyChanged(nameof(AddedUsers));
    }
  }
}
