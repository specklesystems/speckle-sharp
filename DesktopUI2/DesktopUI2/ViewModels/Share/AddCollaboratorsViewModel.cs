using Avalonia.Controls;
using Avalonia.Controls.Selection;
using DesktopUI2.Views.Pages.ShareControls;
using ReactiveUI;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Splat;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DesktopUI2.ViewModels.Share
{
  public class AddCollaboratorsViewModel : ReactiveObject, IRoutableViewModel
  {
    public IScreen HostScreen { get; }
    public string UrlPathSegment { get; } = "collaborators";

    private ConnectorBindings Bindings;

    #region bindings

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
          this.RaisePropertyChanged("AddedUsers");
          this.RaisePropertyChanged("HasAddedUsers");
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

    private bool _showProgress;
    public bool ShowProgress
    {
      get => _showProgress;
      private set
      {
        this.RaiseAndSetIfChanged(ref _showProgress, value);
      }
    }

    public bool HasAddedUsers
    {
      get => AddedUsers.Any();
    }

    public bool HasSelectedUsers
    {
      get => SelectionModel.SelectedItems.Any();
    }

    public SelectionModel<AccountViewModel> SelectionModel { get; private set; }
    #endregion

    public AddCollaboratorsViewModel(IScreen screen)
    {
      HostScreen = screen;
      Bindings = Locator.Current.GetService<ConnectorBindings>();

      userSearchDebouncer = Utils.Debounce(SearchUsers);
      Setup.Init(Bindings.GetHostAppNameVersion(), Bindings.GetHostAppName());

      SelectionModel = new SelectionModel<AccountViewModel>();
      SelectionModel.SingleSelect = false;
      SelectionModel.SelectionChanged += SelectionModel_SelectionChanged;
    }

    private void Search()
    {

      Focus();
      if (SearchQuery.Length < 3)
        return;

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
      var searchBox = AddCollaborators.Instance.FindControl<TextBox>("SearchBox");
      searchBox.Focus();
    }

    private void SelectionModel_SelectionChanged(object sender, SelectionModelSelectionChangedEventArgs<AccountViewModel> e)
    {
      this.RaisePropertyChanged("HasSelectedUsers");
    }

    private async void SearchUsers()
    {
      ShowProgress = true;
      var acc = AccountManager.GetDefaultAccount();

      var client = new Client(acc);
      Users = (await client.UserSearch(SearchQuery)).Select(x => new AccountViewModel(x)).ToList();

      ShowProgress = false;
      DropDownOpen = true;

    }

    private async void ShareCommand()
    {
      ShareViewModel.RouterInstance.Navigate.Execute(new SendingViewModel(HostScreen, AddedUsers.ToList()));
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
      this.RaisePropertyChanged("HasAddedUsers");
    }


  }
}
