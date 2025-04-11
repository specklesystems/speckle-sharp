#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DesktopUI2.ViewModels;
using Material.Dialog.Icons;
using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;

namespace DesktopUI2.Views.Windows.Dialogs;

public sealed class NewStreamDialog : DialogUserControl
{
  private readonly ComboBox _accountsOptions;
  private readonly ComboBox _workspacesOptions;
  private readonly TextBox _name;
  private readonly TextBox _description;
  private readonly ToggleSwitch _isPublic;
  private readonly Button _create;
  private readonly Label _permissionMessage;

  public NewStreamDialog() { }

  public NewStreamDialog(List<AccountViewModel> accounts)
  {
    InitializeComponent();
    _accountsOptions = this.FindControl<ComboBox>("accounts");
    _workspacesOptions = this.FindControl<ComboBox>("workspaces");
    _name = this.FindControl<TextBox>("name");
    _description = this.FindControl<TextBox>("description");
    _isPublic = this.FindControl<ToggleSwitch>("isPublic");
    _create = this.FindControl<Button>("create");
    _permissionMessage = this.FindControl<Label>("permissionMessage");

    InitialiseOptions(accounts);
  }

  private void InitialiseOptions(List<AccountViewModel> accounts)
  {
    _accountsOptions.Items = accounts;
    _accountsOptions.SelectionChanged += OnAccountsOptionsOnSelectionChanged;
    _accountsOptions.SelectedIndex = accounts.FindIndex(x => x.Account.isDefault);

    _workspacesOptions.SelectionChanged += OnWorkspacesOptionsOnSelectionChanged;
  }

  private async void OnAccountsOptionsOnSelectionChanged(object? o, SelectionChangedEventArgs selectionChangedEventArgs)
  {
    try
    {
      await UpdateWorkspaces().ConfigureAwait(true);
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      DesktopUI2.Dialogs.ShowDialog("Something went wrong...", ex.Message, DialogIconKind.Error);
    }
  }

  private async void OnWorkspacesOptionsOnSelectionChanged(object? sender, SelectionChangedEventArgs args)
  {
    try
    {
      await UpdateCreateButton().ConfigureAwait(true);
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      DesktopUI2.Dialogs.ShowDialog("Something went wrong...", ex.Message, DialogIconKind.Error);
    }
  }

  private async Task UpdateCreateButton()
  {
    if (
      _accountsOptions.SelectedItem is not AccountViewModel selectedAccount
      || _workspacesOptions.SelectedItem is not WorkspaceViewModel selectedWorkspace
    )
    {
      _create.IsEnabled = false;
      _permissionMessage.Content = "Select a Workspace";
      return;
    }

    const string READY_MESSAGE = "Ready";
    PermissionCheckResult result;

    if (selectedWorkspace.Workspace is null)
    {
      try
      {
        using Client client = new(selectedAccount.Account);
        result = await client.ActiveUser.CanCreatePersonalProjects().ConfigureAwait(true);
      }
      catch (SpeckleGraphQLException)
      {
        //Expected `GRAPHQL_VALIDATION_FAILED` (old servers)
        _create.IsEnabled = true;
        _permissionMessage.Content = READY_MESSAGE;
        return;
      }
    }
    else
    {
      result = selectedWorkspace.Workspace.permissions.canCreateProject;
    }

    _create.IsEnabled = result.authorized;
    _permissionMessage.Content = result.authorized ? READY_MESSAGE : result.message;
  }

  private async Task UpdateWorkspaces()
  {
    _workspacesOptions.Items = Enumerable.Empty<object>();
    _workspacesOptions.SelectedIndex = -1;

    IEnumerable<WorkspaceViewModel> workspaceViewModels = Enumerable.Empty<WorkspaceViewModel>();
    if (_accountsOptions.SelectedItem is AccountViewModel selectedViewModel)
    {
      using Client client = new(selectedViewModel.Account);
      var workspaces = await TryFetchWorkspaces(client).ConfigureAwait(true);
      workspaceViewModels = workspaces.Select(x => new WorkspaceViewModel(x));

      bool canCreatePersonalProjects;
      try
      {
        var res = await client.ActiveUser.CanCreatePersonalProjects().ConfigureAwait(true);
        canCreatePersonalProjects = res.authorized;
      }
      catch (SpeckleGraphQLException)
      {
        //Expected `GRAPHQL_VALIDATION_FAILED` (old servers)
        canCreatePersonalProjects = true;
      }

      if (canCreatePersonalProjects)
      {
        workspaceViewModels = workspaceViewModels.Prepend(WorkspaceViewModel.PersonalProjects);
        _workspacesOptions.SelectedIndex = 0;
      }
    }

    var items = workspaceViewModels.ToList();
    _workspacesOptions.Items = items;
  }

  public Account Account { get; private set; }

  public Workspace? Workspace { get; private set; }
  public string StreamName { get; private set; }
  public string Description { get; private set; }
  public bool IsPublic { get; private set; }

  private void InitializeComponent()
  {
    AvaloniaXamlLoader.Load(this);
  }

  public void Create_Click(object sender, RoutedEventArgs e)
  {
    //too lazy to create a view model for this or properly style the Dialogs
    Account = ((AccountViewModel)_accountsOptions.SelectedItem).Account;
    StreamName = _name.Text;
    Description = _description.Text;
    IsPublic = _isPublic.IsChecked ?? false;
    Workspace = (_workspacesOptions.SelectedItem as WorkspaceViewModel)?.Workspace;
    Close(true);
  }

  public void Close_Click(object sender, RoutedEventArgs e)
  {
    Close(false);
  }

  private static async Task<IEnumerable<Workspace>> TryFetchWorkspaces(
    Client client,
    CancellationToken cancellationToken = default
  )
  {
    try
    {
      var workspaces = await client.ActiveUser.GetWorkspaces(cancellationToken: cancellationToken).ConfigureAwait(true);
      return workspaces.items;
    }
    catch (SpeckleGraphQLException)
    {
      //Expect `WORKSPACES_MODULE_DISABLED_ERROR` or `GRAPHQL_VALIDATION_FAILED` (old servers)
      return Enumerable.Empty<Workspace>();
    }
  }
}
