using System.Collections.Generic;
using Speckle.Core.Credentials;

namespace DesktopUI2.ViewModels.DesignViewModels;

public class DesignShareViewModel
{
  public DesignShareViewModel()
  {
    SelectedUsers.Add(new AccountViewModel(AccountManager.GetDefaultAccount()));
  }

  public string Title => "for Revit";
  public string TitleFull => "Scheduler for Revit";
  public string SearchQuery => "";

  public List<AccountViewModel> SelectedUsers { get; set; } = new();
}
