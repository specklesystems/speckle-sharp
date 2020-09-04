using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Core.Api.SubscriptionModels
{
  public class StreamInfo
  {
    public string id { get; set; }
    public string name { get; set; }
    public string description { get; set; }
  }

  public class UserStreamAddedResult
  {
    public StreamInfo userStreamAdded { get; set; }
  }

  public class StreamUpdatedResult
  {
    public StreamInfo streamUpdated { get; set; }
  }

  public class UserStreamRemovedResult
  {
    public StreamInfo userStreamRemoved { get; set; }
  }


  public class BranchInfo
  {
    public string id { get; set; }
    public string name { get; set; }
    public string description { get; set; }
    public string streamId { get; set; }
    public string authorId { get; set; }
  }

  public class BranchEventResult
  {
    public BranchInfo branchCreated { get; set; }
  }

  public class BranchUpdatedResult
  {
    public BranchInfo branchUpdated { get; set; }
  }

  public class BranchDeletedResult
  {
    public BranchInfo branchDeleted { get; set; }
  }

}
