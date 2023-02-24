using System;

namespace Speckle.Core.Api.SubscriptionModels
{
  #region streams 
  public class StreamInfo
  {
    public string id { get; set; }
    public string name { get; set; }
    public string description { get; set; }
    public string sharedBy { get; set; }

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
  #endregion

  #region branches

  public class BranchInfo
  {
    public string id { get; set; }
    public string name { get; set; }
    public string description { get; set; }
    public string streamId { get; set; }
    public string authorId { get; set; }
  }

  public class BranchCreatedResult
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
  #endregion

  #region commits

  public class CommitInfo
  {
    public string id { get; set; }
    public string streamId { get; set; }
    public string branchName { get; set; }
    public string objectId { get; set; }
    public string authorId { get; set; }
    public string message { get; set; }
    public string sourceApplication { get; set; }
    public int? totalChildrenCount { get; set; }
    public string[] parents { get; set; }

    [Obsolete("Please use the parents property. This property will be removed in later versions")]
    public string[] previousCommitIds { get; set; }

  }

  public class CommitCreatedResult
  {
    public CommitInfo commitCreated { get; set; }
  }

  public class CommitUpdatedResult
  {
    public CommitInfo commitUpdated { get; set; }
  }

  public class CommitDeletedResult
  {
    public CommitInfo commitDeleted { get; set; }
  }
  #endregion
}
