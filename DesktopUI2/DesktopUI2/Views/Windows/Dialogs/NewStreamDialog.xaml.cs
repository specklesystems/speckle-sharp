#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
using Speckle.Core.Helpers;
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
  private readonly TextBlock _permissionMessage;
  private readonly Button _callToAction;

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
    _permissionMessage = this.FindControl<TextBlock>("permissionMessage");
    _callToAction = this.FindControl<Button>("callToAction");

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
      SetStatus("Select a Workspace", false, false);
      return;
    }

    const string READY_MESSAGE = " ";
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
        SetStatus(READY_MESSAGE, true, false);
        return;
      }
    }
    else
    {
      result = selectedWorkspace.Workspace.permissions.canCreateProject;
    }

    SetStatus(
      result.authorized ? READY_MESSAGE : result.message,
      result.authorized,
      result.code == "WorkspaceLimitsReached"
    );

    return;

    void SetStatus(string message, bool isReady, bool isLimit)
    {
      _create.IsEnabled = isReady;
      _callToAction.IsVisible = isLimit;
      _callToAction.IsEnabled = isLimit;
      _permissionMessage.Text = message;
    }
  }

  private async Task UpdateWorkspaces()
  {
    _workspacesOptions.Items = Enumerable.Empty<object>();

    if (_accountsOptions.SelectedItem is not AccountViewModel selectedViewModel)
    {
      return;
    }

    using Client client = new(selectedViewModel.Account);
    var workspaces = await TryFetchWorkspaces(client).ConfigureAwait(true);
    IEnumerable<WorkspaceViewModel> workspaceViewModels = workspaces.Select(x => new WorkspaceViewModel(x));

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
    }

    _workspacesOptions.Items = workspaceViewModels.ToList();
    _workspacesOptions.SelectedIndex = canCreatePersonalProjects ? 0 : -1;
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

  public void CTA_Click(object sender, RoutedEventArgs e)
  {
    Account = ((AccountViewModel)_accountsOptions.SelectedItem).Account;
    Workspace = ((WorkspaceViewModel)_workspacesOptions.SelectedItem).Workspace;
    Open.Url($"{Account.serverInfo.url}/settings/workspaces/{Workspace.slug}/billing");
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
