using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Speckle.Http;
using Speckle.Transports;

namespace Speckle.Core
{
  public partial class Remote
  {
    public Account Account { get; set; }

    public string Name { get; set; }

    public Stream RemoteStream { get; set; }

    [JsonIgnore]
    public Stream LocalStream { get; set; }

    public MockServerClient ServerClient { get; set; }

    public Remote() { } 

    public Remote(Account account, string name)
    {
      this.Account = account;
      this.Name = name;

      ServerClient = new MockServerClient(account.ServerUrl, account.ApiToken);
    }

    /// <summary>
    /// Pushes the specified commit to a given speckle remote server.
    /// </summary>
    /// <param name="branchName">The branch you want to push.</param>
    /// <param name="commit">The specific commit from that branch that you want to push. Defaults to the branch's head.</param>
    /// <param name="preserveHistory"></param>
    /// <param name="OnProgress"></param>
    public void Push(string branchName, string commit, bool preserveHistory, EventHandler<ProgressEventArgs> OnProgress = null)
    {
      if (preserveHistory)
        throw new Exception("Preserve history is not yet supported.");

      OnProgress?.Invoke(this, new ProgressEventArgs(1, 1, $"Pushing to remote {Name}"));

      var branch = LocalStream.Branches.Find(b => b.Name == branchName);

      var shallowCommit = JsonConvert.DeserializeObject<ShallowCommit>(LocalStream.LocalObjectTransport.GetObject(commit != null ? commit : branch.Head));

      var allObjIdsToPush = shallowCommit.GetAllObjects();

      // POST /streams/{LocalStream.id} <-- LocalStream is it needed?
      // POST MANY /streams/objects <-- allObjIdsToPush (pending preflight check, including commit)
      // POST /streams/commit
      // POST /streams/{LocalStream.id}/ref/{ref.Name} <-- branch


      // Steps:
      // 
      // Get remote stream / do a preflight check 

      // Update remote stream - create branch & tags
      //throw new NotImplementedException();
    }




    public void Fetch()
    {
      throw new NotImplementedException();
    }

    public void Pull()
    {

    }


  }
}
