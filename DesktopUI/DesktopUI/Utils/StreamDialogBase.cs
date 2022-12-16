using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MaterialDesignThemes.Wpf;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Stylet;

namespace Speckle.DesktopUI.Utils
{
  public abstract class StreamDialogBase : Conductor<IScreen>.Collection.OneActive
  {
    protected ConnectorBindings Bindings;

    private int _selectedSlide;

    public int SelectedSlide
    {
      get => _selectedSlide;
      set => SetAndNotify(ref _selectedSlide, value);
    }

    internal Account _accountToSendFrom = AccountManager.GetDefaultAccount();

    public virtual Account AccountToSendFrom
    {
      get => _accountToSendFrom;
      set => SetAndNotify(ref _accountToSendFrom, value);
    }

    private int _selectionCount = 0;

    public int SelectionCount
    {
      get => _selectionCount;
      set => SetAndNotify(ref _selectionCount, value);
    }

    public string ActiveViewName
    {
      get => Bindings.GetActiveViewName();
    }

    public List<string> ActiveViewObjects
    {
      get => Bindings.GetObjectsInView();
    }

    public List<string> CurrentSelection
    {
      get => Bindings.GetSelectedObjects();
    }

    private BindableCollection<FilterTab> _filterTabs;

    public BindableCollection<FilterTab> FilterTabs
    {
      get => _filterTabs;
      set => SetAndNotify(ref _filterTabs, value);
    }

    private FilterTab _selectedFilterTab;

    public FilterTab SelectedFilterTab
    {
      get => _selectedFilterTab;
      set
      {
        SetAndNotify(ref _selectedFilterTab, value);
        _selectedFilterTab.RestoreSelectedItems();
      }
    }


    #region Adding Collaborators

    private string _userQuery;

    public string UserQuery
    {
      get => _userQuery;
      set
      {
        SetAndNotify(ref _userQuery, value);

        if (value == "")
        {
          SelectedUser = null;
          UserSearchResults?.Clear();
        }

        if (SelectedUser != null) return;
        SearchForUsers();
      }
    }

    private BindableCollection<LimitedUser> _userSearchResults;

    public BindableCollection<LimitedUser> UserSearchResults
    {
      get => _userSearchResults;
      set => SetAndNotify(ref _userSearchResults, value);
    }

    private User _selectedUser;

    public User SelectedUser
    {
      get => _selectedUser;
      set
      {
        SetAndNotify(ref _selectedUser, value);
        if (SelectedUser == null)
          return;
        UserQuery = SelectedUser.name;
        AddCollabToCollection(SelectedUser);
      }
    }

    public async void SearchForUsers()
    {
      if (UserQuery == null || UserQuery.Length <= 2)
        return;

      try
      {
        var client = new Client(AccountToSendFrom);
        var users = await client.UserSearch(UserQuery);
        UserSearchResults = new BindableCollection<LimitedUser>(users);
      }
      catch (Exception e)
      {
        // search prob returned no results
        UserSearchResults?.Clear();
      }
    }

    private StreamRole _role;

    public StreamRole Role
    {
      get => _role;
      set => SetAndNotify(ref _role, value);
    }

    public BindableCollection<StreamRole> Roles { get; internal set; }

    private BindableCollection<User> _collaborators = new BindableCollection<User>();

    public BindableCollection<User> Collaborators
    {
      get => _collaborators;
      set => SetAndNotify(ref _collaborators, value);
    }

    private void AddCollabToCollection(User user)
    {
      if (Collaborators.All(c => c.id != user.id))
        Collaborators.Add(user);
    }

    public void RemoveCollabFromCollection(User user)
    {
      Collaborators.Remove(user);
    }

    #endregion

    public void CloseDialog()
    {
      DialogHost.CloseDialogCommand.Execute(null, null);
    }
  }

  public class StreamRole
  {
    public StreamRole(string name, string description)
    {
      Name = name;
      Role = $"stream:{name.ToLower()}";
      Description = description;
    }

    public string Name { get; }
    public string Role { get; }
    public string Description { get; }
  }
}
