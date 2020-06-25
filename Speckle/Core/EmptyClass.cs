using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Speckle.Models;
using Speckle.Serialisation;
using Speckle.Transports;

namespace Speckle.Core
{

  public class Stream
  {
    [JsonIgnore]
    public ITransport LocalObjectTransport { get; set; }

    [JsonIgnore]
    public ITransport LocalStreamTransport { get; set; }

    public string PreviousCommitId { get; set; }

    public Stream()
    {
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

    public string Email { get; set; }

    public string ApiToken { get; set; }

    public Remote() { }
  }


}
