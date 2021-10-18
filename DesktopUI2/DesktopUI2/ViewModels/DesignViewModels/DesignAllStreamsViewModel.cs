using Speckle.Core.Api;
using Speckle.Core.Credentials;
using System;
using System.Collections.Generic;
using System.Text;

namespace DesktopUI2.ViewModels.DesignViewModels
{
  public class DesignAllStreamsViewModel
  {
    public bool InProgress = false;

    public Account SelectedAccount = null;

    public List<Account> Accounts = new List<Account>();

    public List<Stream> Streams = new List<Stream>();
    public DesignAllStreamsViewModel()
    {
      var acc = AccountManager.GetDefaultAccount();
      var client = new Client(acc);
      Streams = client.StreamsGet().Result;
    }

    public void NewStreamCommand()
    {

    }

    public void AddFromUrlCommand()
    {
    }
  }
}
