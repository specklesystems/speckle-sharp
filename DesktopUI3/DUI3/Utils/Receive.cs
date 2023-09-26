using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DUI3.Bindings;
using DUI3.Models;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;

namespace DUI3.Utils;

public static class Receive
{
  public static async Task<Base> GetCommitBase(IBridge parent, DocumentModelStore store, CancellationToken token, string modelCardId, string versionId)
  {
    ReceiverModelCard receiverModelCard = store.GetModelById(modelCardId) as ReceiverModelCard;

    if (receiverModelCard == null)
    {
      // TODO: Trigger UI with message
      // parent.SendToBrowser(...);
    }
    
    Account account = AccountManager.GetAccounts().Where(acc => acc.id == receiverModelCard.AccountId).FirstOrDefault();
    if (account == null)
    {
      // TODO: Trigger UI with message
      // parent.SendToBrowser(...);
    }
    Client client = new(account);
      
    Commit version = await client.CommitGet(receiverModelCard.ProjectId, versionId).ConfigureAwait(false);

    Base commitObject = await DUI3.Operations.Operations.ReceiveCommit(
      account,
      token,
      receiverModelCard.ProjectId,
      version.referencedObject).ConfigureAwait(true);
    return commitObject;
  }
}
