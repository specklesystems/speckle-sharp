using Speckle.Core.Credentials;
using System.Collections.Generic;

namespace DesktopUI2.ViewModels.DesignViewModels
{
  public class DesignShareViewModel
  {
    public string Title => "for Revit";
    public string TitleFull => "Scheduler for Revit";
    public string SearchQuery => "";

    public List<AccountViewModel> SelectedUsers { get; set; } = new List<AccountViewModel>();

    public DesignShareViewModel()
    {
      SelectedUsers.Add(new AccountViewModel(AccountManager.GetDefaultAccount()));
    }


  }


}
