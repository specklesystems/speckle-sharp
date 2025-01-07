#nullable disable
using System;
using System.Collections.Generic;

namespace Speckle.Core.Api.SubscriptionModels;

#region streams
[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class StreamInfo
{
  public string id { get; set; }
  public string name { get; set; }
  public string description { get; set; }
  public string sharedBy { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class UserStreamAddedResult
{
  public StreamInfo userStreamAdded { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class StreamUpdatedResult
{
  public StreamInfo streamUpdated { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class UserStreamRemovedResult
{
  public StreamInfo userStreamRemoved { get; set; }
}
#endregion

#region branches

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class BranchInfo
{
  public string id { get; set; }
  public string name { get; set; }
  public string description { get; set; }
  public string streamId { get; set; }
  public string authorId { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class BranchCreatedResult
{
  public BranchInfo branchCreated { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class BranchUpdatedResult
{
  public BranchInfo branchUpdated { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class BranchDeletedResult
{
  public BranchInfo branchDeleted { get; set; }
}
#endregion

#region commits

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
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
  public IList<string> parents { get; set; }

  [Obsolete("Please use the parents property. This property will be removed in later versions")]
  public IList<string> previousCommitIds { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class CommitCreatedResult
{
  public CommitInfo commitCreated { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class CommitUpdatedResult
{
  public CommitInfo commitUpdated { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class CommitDeletedResult
{
  public CommitInfo commitDeleted { get; set; }
}
#endregion
