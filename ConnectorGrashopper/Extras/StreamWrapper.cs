using Speckle.Core.Credentials;
using System.Linq;

namespace ConnectorGrashopper.Extras
{
  public class StreamWrapper
  {
    public StreamWrapper() { }

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
      return $"{ServerUrl}/streams/{StreamId}@{AccountId}";
    }

    public Account GetAccount()
    {
      Account account = null;

      account = AccountManager.GetAccounts().FirstOrDefault(a => a.id == AccountId);
      if (account == null)
      {
        account = AccountManager.GetAccounts(ServerUrl).FirstOrDefault();
      }

      if (account != null)
      {
        AccountId = account.id;
      }

      return account;
    }
  }
}
