using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Speckle.Models;
using Speckle.Serialisation;
using Speckle.Transports;

namespace Speckle.Core2
{

  public class Stream
  {
    [JsonIgnore]
    public ITransport LocalObjectTransport { get; set; }

    [JsonIgnore]
    public ITransport LocalStreamTransport { get; set; }

    public Serializer Serializer { get; set; }

    public Stream()
    {
      Serializer = new Serializer();
      LocalObjectTransport = new DiskTransport();
      LocalStreamTransport = new DiskTransport(scope: "Streams", splitPath: false);
    }

    public string PreviousCommitId { get; set; }

    public async Task<string> PushLocal(List<Base> objects, string branchName = "master", string commitMessage = "")
    {
      var commit = new Commit { Objects = objects, CommitMessage = commitMessage };

      Serializer.SerializeAndSave(commit, LocalObjectTransport);

      PreviousCommitId = commit.hash;
      return PreviousCommitId;
    }

    public async Task<List<Base>> PullLocal(string commitId)
    {
      var commit = (Commit) Serializer.DeserializeAndGet(LocalObjectTransport.GetObject(commitId), LocalObjectTransport);

      return commit.Objects;
    }

  }

  public class Commit : Base
  {
    [DetachProperty]
    public List<Base> Objects { get; set; }

    public string CommitMessage { get; set; }

    public Commit() { }
  }

  public class Remote
  {
    public string ServerUrl { get; set; }

    public string StreamId { get; set; }

    public Remote() { }

    public async Task Push(string commitId)
    {

    }
  }


}
