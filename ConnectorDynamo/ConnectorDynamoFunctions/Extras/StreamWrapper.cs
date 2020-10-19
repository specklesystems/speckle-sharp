using Speckle.Core.Credentials;
using Account = Speckle.Core.Credentials.Account;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.DesignScript.Runtime;

namespace Speckle.ConnectorDynamo.Functions.Extras
{
  [IsVisibleInDynamoLibrary(false)]
  public class StreamWrapper
  {
    public StreamWrapper(string streamId, string accountId, string serverUrl)
    {
      StreamId = streamId;
      AccountId = accountId;
      ServerUrl = serverUrl;
    }

    public string StreamId { get; set; }
    public string AccountId { get; set; }
    public string ServerUrl { get; set; }

    public override string ToString()
    {
      return $"Id: {StreamId} @ {ServerUrl}";
    }

    public Core.Credentials.Account GetAccount()
    {
      Core.Credentials.Account account = null;

      account = AccountManager.GetAccounts().FirstOrDefault(a => a.id == AccountId);
      if (account == null)
      {
        account = AccountManager.GetAccounts(ServerUrl).FirstOrDefault();
      }

      return account;
    }
  }
}
