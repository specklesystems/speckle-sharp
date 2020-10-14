using Grasshopper.Kernel;
using Speckle.Core.Credentials;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConnectorGrashopper.Extras
{
  public class StreamWrapper
  {
    public string StreamId { get; set; }
    public string AccountId { get; set; }
    public string ServerUrl { get; set; }

    public override string ToString()
    {
      return $"Id: {StreamId} @ {ServerUrl}";
    }

    public Account GetAccount()
    {
      Account account = null;

      account = AccountManager.GetAccounts().FirstOrDefault(a => a.id == AccountId);
      if (account == null)
      {
        account = AccountManager.GetAccounts(ServerUrl).FirstOrDefault();
      }

      return account;
    }
  }
}
