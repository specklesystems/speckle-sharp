using DesktopUI2.Models;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using System.Collections.Generic;
using System.Linq;

namespace DesktopUI2.ViewModels.DesignViewModels
{
  public class DesignAllStreamsViewModel
  {
    public bool InProgress { get; set; } = false;

    public Account SelectedAccount { get; set; } = null;

    public List<Account> Accounts { get; set; } = new List<Account>();

    public string SearchQuery { get; set; }

    public List<StreamAccountWrapper> Streams { get; set; } = new List<StreamAccountWrapper>();

    public DesignAllStreamsViewModel()
    {
      var acc = AccountManager.GetDefaultAccount();
      var client = new Client(acc);
      Streams = client.StreamsGet().Result.Select(x => new StreamAccountWrapper(x, acc)).ToList();
    }

    public void NewStreamCommand()
    {

    }

    public void AddFromUrlCommand()
    {
    }
  }
}
