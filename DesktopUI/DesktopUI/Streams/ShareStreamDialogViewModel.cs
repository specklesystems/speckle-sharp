using System;
using System.Diagnostics;
using System.Threading.Tasks;
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

    public async void SearchForUsers()
    {
      if (UserQuery.Length <= 2)
        return;

      try
      {
        var users = await StreamState.Client.UserSearch(UserQuery);
        UserSearchResults = new BindableCollection<User>(users);
      }
      catch (Exception e)
      {
        // search prob returned no results
        UserSearchResults?.Clear();
      }
    }

    // TODO extract dialog logic into separate manager to better handle open / close
    public void CloseDialog()
    {
      DialogHost.CloseDialogCommand.Execute(null, null);
    }
  }
}
