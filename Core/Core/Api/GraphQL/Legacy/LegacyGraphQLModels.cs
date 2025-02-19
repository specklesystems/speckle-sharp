#nullable disable
using System;
using System.Collections.Generic;
using Speckle.Core.Api.GraphQL.Enums;
using Speckle.Core.Api.GraphQL.Models;

namespace Speckle.Core.Api;

#region inputs

internal static class DeprecationMessages
{
  public const string FE1_DEPRECATION_MESSAGE =
    $"Stream/Branch/Commit API is now deprecated, Use the new Project/Model/Version API functions in {nameof(Client)}";
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class StreamCreateInput
{
  public string name { get; set; }
  public string description { get; set; }
  public bool isPublic { get; set; } = true;
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class StreamUpdateInput
{
  public string id { get; set; }
  public string name { get; set; }
  public string description { get; set; }
  public bool isPublic { get; set; } = true;
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class StreamPermissionInput
{
  public string streamId { get; set; }
  public string userId { get; set; }
  public string role { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class StreamRevokePermissionInput
{
  public string streamId { get; set; }
  public string userId { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class StreamInviteCreateInput
{
  public string streamId { get; set; }
  public string userId { get; set; }
  public string email { get; set; }
  public string message { get; set; }
  public string role { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class BranchCreateInput
{
  public string streamId { get; set; }
  public string name { get; set; }
  public string description { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class BranchUpdateInput
{
  public string streamId { get; set; }
  public string id { get; set; }
  public string name { get; set; }
  public string description { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class BranchDeleteInput
{
  public string streamId { get; set; }
  public string id { get; set; }
}

public class CommitCreateInput
{
  public string streamId { get; set; }
  public string branchName { get; set; }
  public string objectId { get; set; }
  public string message { get; set; }
  public string sourceApplication { get; set; } = ".net";
  public int totalChildrenCount { get; set; }
  public List<string> parents { get; set; }

  [Obsolete("Please use the parents property. This property will be removed in later versions")]
  public List<string> previousCommitIds { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class CommitUpdateInput
{
  public string streamId { get; set; }
  public string id { get; set; }
  public string message { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class CommitDeleteInput
{
  public string streamId { get; set; }
  public string id { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class CommitReceivedInput
{
  public string streamId { get; set; }
  public string commitId { get; set; }
  public string sourceApplication { get; set; }
  public string message { get; set; }
}

#endregion

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class Stream
{
  public string id { get; set; }
  public string name { get; set; }
  public string description { get; set; }

  public bool isPublic { get; set; }
  public string role { get; set; }
  public DateTime createdAt { get; set; }
  public DateTime updatedAt { get; set; }
  public string favoritedDate { get; set; }

  public int commentCount { get; set; }
  public int favoritesCount { get; set; }

  public List<Collaborator> collaborators { get; set; }
  public List<PendingStreamCollaborator> pendingCollaborators { get; set; } = new();
  public Branches branches { get; set; }

  /// <summary>
  /// Set only in the case that you've requested this through <see cref="Client.BranchGet(string, string, int, System.Threading.CancellationToken)"/>.
  /// </summary>
  public Branch branch { get; set; }

  /// <summary>
  /// Set only in the case that you've requested this through <see cref="Client.CommitGet(string, string, System.Threading.CancellationToken)"/>.
  /// </summary>
  public Commit commit { get; set; }

  /// <summary>
  /// Set only in the case that you've requested this through <see cref="Client.StreamGetCommits(string, int, System.Threading.CancellationToken)"/>
  /// </summary>
  public Commits commits { get; set; }

  public Activity activity { get; set; }

  public SpeckleObject @object { get; set; }

  public override string ToString()
  {
    return $"Stream ({name} | {id})";
  }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class Collaborator
{
  public string id { get; set; }
  public string name { get; set; }
  public string role { get; set; }
  public string avatar { get; set; }

  public override string ToString()
  {
    return $"Collaborator ({name} | {role} | {id})";
  }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class StreamInvitesResponse
{
  public List<PendingStreamCollaborator> streamInvites { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class Branches
{
  public int totalCount { get; set; }
  public string cursor { get; set; }
  public List<Branch> items { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class Commits
{
  public int totalCount { get; set; }
  public string cursor { get; set; }
  public List<Commit> items { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class Commit
{
  public string id { get; set; }
  public string message { get; set; }
  public string branchName { get; set; }
  public string authorName { get; set; }
  public string authorId { get; set; }
  public string authorAvatar { get; set; }
  public DateTime createdAt { get; set; }
  public string sourceApplication { get; set; }

  public string referencedObject { get; set; }
  public int totalChildrenCount { get; set; }
  public List<string> parents { get; set; }

  public override string ToString()
  {
    return $"Commit ({message} | {id})";
  }
}

public class Activity
{
  public int totalCount { get; set; }
  public DateTime cursor { get; set; }
  public List<ActivityItem> items { get; set; }
}

public class ActivityItem
{
  public string actionType { get; set; }
  public string userId { get; set; }
  public string streamId { get; set; }
  public string resourceId { get; set; }
  public string resourceType { get; set; }
  public DateTime time { get; set; }
  public Info info { get; set; }
  public string message { get; set; }
}

public class Info
{
  public string message { get; set; }
  public string sourceApplication { get; set; }

  public InfoCommit commit { get; set; }
}

public class InfoCommit
{
  public string message { get; set; }
  public string sourceApplication { get; set; }
  public string branchName { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class SpeckleObject
{
  public string id { get; set; }
  public string speckleType { get; set; }
  public string applicationId { get; set; }
  public int? totalChildrenCount { get; set; }
  public DateTime createdAt { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class Branch
{
  public string id { get; set; }
  public string name { get; set; }
  public string description { get; set; }
  public Commits commits { get; set; }

  public override string ToString()
  {
    return $"Branch ({name} | {id})";
  }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class Streams
{
  public int totalCount { get; set; }
  public string cursor { get; set; }
  public List<Stream> items { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class Resource
{
  public string resourceId { get; set; }
  public ResourceType resourceType { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class Location
{
  public double x { get; set; }
  public double y { get; set; }
  public double z { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class UserSearchData
{
  public UserSearch userSearch { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class UserSearch
{
  public string cursor { get; set; }
  public List<LimitedUser> items { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class StreamData
{
  public Stream stream { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class StreamsData
{
  public Streams streams { get; set; }
}

#region comments
[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class Comments
{
  public int totalCount { get; set; }
  public DateTime? cursor { get; set; }
  public List<CommentItem> items { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public sealed class CommentData
{
  public Comments comments { get; init; }
  public List<double> camPos { get; init; }
  public object filters { get; init; }
  public Location location { get; init; }
  public object selection { get; init; }
  public object sectionBox { get; init; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class CommentItem
{
  public string id { get; set; }
  public string authorId { get; set; }
  public bool archived { get; set; }
  public string screenshot { get; set; }
  public string rawText { get; set; }
  public CommentData data { get; set; }
  public DateTime createdAt { get; set; }
  public DateTime updatedAt { get; set; }
  public DateTime? viewedAt { get; set; }
  public object reactions { get; set; }
  public Comments replies { get; set; }
  public List<Resource> resources { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class ContentContent
{
  public string Type { get; set; }

  //public Mark[] Marks { get; set; }
  public string Text { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class CommentsData
{
  public Comments comments { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class CommentItemData
{
  public CommentItem comment { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class CommentActivityMessage
{
  public string type { get; set; }
  public CommentItem comment { get; set; }
}

[Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
public class CommentActivityResponse
{
  public CommentActivityMessage commentActivity { get; set; }
}
#endregion
