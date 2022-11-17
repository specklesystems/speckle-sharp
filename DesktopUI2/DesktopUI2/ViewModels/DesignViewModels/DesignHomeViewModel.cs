using DesktopUI2.Models;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using System.Collections.Generic;
using System.Linq;

namespace DesktopUI2.ViewModels.DesignViewModels
{
  public class DesignHomeViewModel
  {
    public bool InProgress { get; set; } = false;

    public Account SelectedAccount { get; set; } = null;

    public bool HasAccounts { get; set; } = true;
    public bool HasMultipleAccounts { get; set; } = true;
    public bool HasOneAccount { get; set; } = false;

    public List<AccountViewModel> Accounts { get; set; } = new List<AccountViewModel>();

    public string SearchQuery { get; set; }

    public List<StreamAccountWrapper> FilteredStreams { get; set; } = new List<StreamAccountWrapper>();

    public List<DesignSavedStreamViewModel> SavedStreams { get; set; }

    public bool HasSavedStreams { get; set; } = true;

    public string Title { get; set; } = "for Desktop UI 3D Max";
    public string Version { get; set; } = "1.2.3-beta8";

    public DesignHomeViewModel()
    {
      var acc = AccountManager.GetDefaultAccount();
      Accounts = AccountManager.GetAccounts().Select(x => new AccountViewModel(x)).ToList();
      if (acc == null)
        return;
      var client = new Client(acc);
      FilteredStreams = client.StreamsGet().Result.Select(x => new StreamAccountWrapper(x, acc)).ToList();

      var d = new DesignSavedStreamsViewModel();
      SavedStreams = d.SavedStreams;
      //SavedStreams = new List<SavedStreamViewModel>();

      //var streamState = new StreamState(Streams.First());
      //var savedState = new SavedStreamViewModel(streamState, null, null);
      //SavedStreams.Add(savedState);
    }

    public void NewStreamCommand()
    {

    }

    public void AddFromUrlCommand()
    {
    }

    public void AddAccountCommand()
    {
    }

    public void ToggleDarkThemeCommand()
    {
    }

    public void RefreshCommand()
    {
    }

    public void ClearSearchCommand() { }
    public void LaunchManagerCommand() { }
    public void DirectAddAccountCommand() { }

    public void OpenProfileCommand(Account account) { }
  }


}
