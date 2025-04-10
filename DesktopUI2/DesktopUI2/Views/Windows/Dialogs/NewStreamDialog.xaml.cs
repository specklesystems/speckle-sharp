#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DesktopUI2.ViewModels;
using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Credentials;

namespace DesktopUI2.Views.Windows.Dialogs;

public sealed class NewStreamDialog : DialogUserControl
{
  private readonly ComboBox _accountsOptions;
  private readonly ComboBox _workspacesOptions;
  private readonly TextBox _name;
  private readonly TextBox _description;
  private readonly ToggleSwitch _isPublic;

  public NewStreamDialog() { }

  public NewStreamDialog(List<AccountViewModel> accounts)
  {
    InitializeComponent();
    _accountsOptions = this.FindControl<ComboBox>("accounts");
    _workspacesOptions = this.FindControl<ComboBox>("workspaces");
    _name = this.FindControl<TextBox>("name");
    _description = this.FindControl<TextBox>("description");
    _isPublic = this.FindControl<ToggleSwitch>("isPublic");

    InitialiseOptions(accounts);
  }

  private void InitialiseOptions(List<AccountViewModel> accounts)
  {
    _accountsOptions.Items = accounts;
    _accountsOptions.SelectionChanged += async (_, _) =>
    {
      _workspacesOptions.Items = Enumerable.Empty<object>();

      IEnumerable<WorkspaceViewModel> workspaceViewModels = Enumerable.Empty<WorkspaceViewModel>();
      if (_accountsOptions.SelectedItem is AccountViewModel accountViewModel)
      {
        using Client client = new(accountViewModel.Account);
        var workspaces = await TryFetchWorkspaces(client).ConfigureAwait(true);
        workspaceViewModels = workspaces.Select(x => new WorkspaceViewModel(x));

        bool canCreatePersonalProjects = true;
        try
        {
          var res = await client.ActiveUser.CanCreatePersonalProjects().ConfigureAwait(true);
          canCreatePersonalProjects = res.authorized;
        }
        catch (SpeckleGraphQLException)
        {
          //Expected `GRAPHQL_VALIDATION_FAILED` (old servers)
        }

        if (canCreatePersonalProjects)
        {
          workspaceViewModels = workspaceViewModels.Prepend(WorkspaceViewModel.PersonalProjects);
        }
      }

      var items = workspaceViewModels.ToList();
      _workspacesOptions.Items = items;
      _workspacesOptions.SelectedIndex = items.Count > 0 ? 0 : -1;
    };

    _accountsOptions.SelectedIndex = accounts.FindIndex(x => x.Account.isDefault);
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
      return workspaces.items.Where(w => w.permissions.canCreateProject.authorized);
    }
    catch (SpeckleGraphQLException)
    {
      //Expect `WORKSPACES_MODULE_DISABLED_ERROR` or `GRAPHQL_VALIDATION_FAILED` (old servers)
      return Enumerable.Empty<Workspace>();
    }
  }
}
