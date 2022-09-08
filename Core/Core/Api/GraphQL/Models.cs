﻿using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Speckle.Core.Api
{
  #region inputs

  public class StreamCreateInput
  {
    public string name { get; set; }
    public string description { get; set; }
    public bool isPublic { get; set; } = true;
  }

  public class StreamUpdateInput
  {
    public string id { get; set; }
    public string name { get; set; }
    public string description { get; set; }
    public bool isPublic { get; set; } = true;
  }

  public class StreamPermissionInput
  {
    public string streamId { get; set; }
    public string userId { get; set; }
    public string role { get; set; }
  }

  public class StreamRevokePermissionInput
  {
    public string streamId { get; set; }
    public string userId { get; set; }
  }

  public class StreamInviteCreateInput
  {
    public string streamId { get; set; }
    public string userId { get; set; }
    public string email { get; set; }
    public string message { get; set; }
  }

  public class BranchCreateInput
  {
    public string streamId { get; set; }
    public string name { get; set; }
    public string description { get; set; }
  }

  public class BranchUpdateInput
  {
    public string streamId { get; set; }
    public string id { get; set; }
    public string name { get; set; }
    public string description { get; set; }
  }

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

  public class CommitUpdateInput
  {
    public string streamId { get; set; }
    public string id { get; set; }
    public string message { get; set; }
  }

  public class CommitDeleteInput
  {
    public string streamId { get; set; }
    public string id { get; set; }
  }

  public class CommitReceivedInput
  {
    public string streamId { get; set; }
    public string commitId { get; set; }
    public string sourceApplication { get; set; }
    public string message { get; set; }
  }

  #endregion

  public class Stream
  {
    public string id { get; set; }
    public string name { get; set; }
    public string description { get; set; }

    public bool isPublic { get; set; }
    public string role { get; set; }
    public string createdAt { get; set; }
    public string updatedAt { get; set; }
    public string favoritedDate { get; set; }

    public int commentCount { get; set; }
    public int favoritesCount { get; set; }

    public List<Collaborator> collaborators { get; set; }
    public Branches branches { get; set; }

    /// <summary>
    /// Set only in the case that you've requested this through <see cref="Client.BranchGet(System.Threading.CancellationToken, string, string, int)"/>.
    /// </summary>
    public Branch branch { get; set; }

    /// <summary>
    /// Set only in the case that you've requested this through <see cref="Client.CommitGet(System.Threading.CancellationToken, string, string)"/>.
    /// </summary>
    public Commit commit { get; set; }

    /// <summary>
    /// Set only in the case that you've requested this through <see cref="Client.StreamGetCommits(System.Threading.CancellationToken, string, int)"/>
    /// </summary>
    public Commits commits { get; set; }

    public Activity activity { get; set; }

    public SpeckleObject @object { get; set; }

    public override string ToString()
    {
      return $"Stream ({name} | {id})";
    }
  }

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

  public class StreamInvitesResponse
  {
    public List<PendingStreamCollaborator> streamInvites { get; set; }
  }
  public class PendingStreamCollaborator
  {
    public string id { get; set; }
    public string inviteId { get; set; }
    public string streamId { get; set; }
    public string streamName { get; set; }
    public string title { get; set; }
    public string role { get; set; }
    public User invitedBy { get; set; }
    public string token { get; set; }
  }

  public class Branches
  {
    public int totalCount { get; set; }
    public string cursor { get; set; }
    public List<Branch> items { get; set; }
  }

  public class Commits
  {
    public int totalCount { get; set; }
    public string cursor { get; set; }
    public List<Commit> items { get; set; }
  }

  public class Commit
  {
    public string id { get; set; }
    public string message { get; set; }
    public string branchName { get; set; }
    public string authorName { get; set; }
    public string authorId { get; set; }
    public string authorAvatar { get; set; }
    public string createdAt { get; set; }
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
    public string time { get; set; }
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

  public class SpeckleObject
  {
    public string id { get; set; }
    public string speckleType { get; set; }
    public string applicationId { get; set; }
    public int totalChildrenCount { get; set; }
    public string createdAt { get; set; }
  }

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

  public class Streams
  {
    public int totalCount { get; set; }
    public string cursor { get; set; }
    public List<Stream> items { get; set; }
  }

  public class User
  {
    public string id { get; set; }
    public string email { get; set; }
    public string name { get; set; }
    public string bio { get; set; }
    public string company { get; set; }
    public string avatar { get; set; }

    public bool verified { get; set; }

    //public object profiles { get; set; }
    public string role { get; set; }
    public Streams streams { get; set; }
    public Streams favoriteStreams { get; set; }

    public override string ToString()
    {
      return $"User ({email} | {name} | {id})";
    }
  }

  public class Comments
  {
    public int totalCount { get; set; }
    public DateTime? cursor { get; set; }
    public List<CommentItem> items { get; set; }
  }

  public class CommentData
  {
    public Comments comments { get; set; }
    public List<double> camPos { get; set; }
    public object filters { get; set; }
    public Location location { get; set; }
    public object selection { get; set; }
    public object sectionBox { get; set; }
  }

  public class CommentItem
  {
    public string id { get; set; }
    public string authorId { get; set; }
    public bool archived { get; set; }
    public string screenshot { get; set; }
    public Text text { get; set; }
    public CommentData data { get; set; }
    public DateTime createdAt { get; set; }
    public DateTime updatedAt { get; set; }
    public DateTime? viewedAt { get; set; }
    public object reactions { get; set; }
    public Comments replies { get; set; }
    public List<Resource> resources { get; set; }
  }

  public partial class Text
  {
    public Doc Doc { get; set; }
  }

  public partial class Doc
  {
    public string Type { get; set; }
    public DocContent[] Content { get; set; }
  }

  public partial class DocContent
  {
    public string Type { get; set; }
    public ContentContent[] Content { get; set; }
  }

  public partial class ContentContent
  {
    public string Type { get; set; }
    //public Mark[] Marks { get; set; }
    public string Text { get; set; }
  }

  public class Resource
  {
    public string resourceId { get; set; }
    public ResourceType resourceType { get; set; }
  }

  public enum ResourceType
  {
    commit,
    stream,
    @object,
    comment
  }

  public class Location
  {
    public double x { get; set; }
    public double y { get; set; }
    public double z { get; set; }
  }

  public class UserData
  {
    public User user { get; set; }
  }

  public class UserSearchData
  {
    public UserSearch userSearch { get; set; }
  }

  public class UserSearch
  {
    public string cursor { get; set; }
    public List<User> items { get; set; }
  }

  public class ServerInfoResponse
  {
    // TODO: server and user models are duplicated here and in Core.Credentials.Responses
    // a bit weird and unnecessary - shouldn't both Credentials and Api share the same models since they're
    // all server models that should be consistent? am creating a new obj here as to not reference Credentials in
    // this file but it should prob be refactored in the futrue
    public ServerInfo serverInfo { get; set; }
  }

  // TODO: prob remove and bring one level up and shared w Core.Credentials
  public class ServerInfo
  {
    public string name { get; set; }
    public string company { get; set; }
    public string url { get; set; }
    public string version { get; set; }
    public string adminContact { get; set; }
    public string description { get; set; }
  }

  public class StreamData
  {
    public Stream stream { get; set; }
  }

  public class StreamsData
  {
    public Streams streams { get; set; }
  }

  public class CommentsData
  {
    public Comments comments { get; set; }
  }

  public class CommentItemData
  {
    public CommentItem comment { get; set; }
  }

  #region manager api

  public class Connector
  {
    public List<Version> Versions { get; set; } = new List<Version>();
  }

  public class Version
  {
    public string Number { get; set; }
    public string Url { get; set; }
    public Os Os { get; set; }
    public Architecture Architecture { get; set; } = Architecture.Any;
    public DateTime Date { get; set; }

    [JsonIgnore]
    public string DateTimeAgo => Helpers.TimeAgo(Date);
    public bool Prerelease { get; set; } = false;

    public Version(string number, string url, Os os = Os.Win, Architecture architecture = Architecture.Any)
    {
      Number = number;
      Url = url;
      Date = DateTime.Now;
      Prerelease = Number.Contains("-");
      Os = os;
      Architecture = architecture;
    }
  }

  public enum Os
  {
    Win,
    OSX
  }

  public enum Architecture
  {
    Any,
    Arm,
    Intel
  }


  //GHOST API
  public class Meta
  {
    public Pagination pagination { get; set; }
  }

  public class Pagination
  {
    public int page { get; set; }
    public string limit { get; set; }
    public int pages { get; set; }
    public int total { get; set; }
    public object next { get; set; }
    public object prev { get; set; }
  }

  public class Tags
  {
    public List<Tag> tags { get; set; }
    public Meta meta { get; set; }
  }

  public class Tag
  {
    public string id { get; set; }
    public string name { get; set; }
    public string slug { get; set; }
    public string description { get; set; }
    public string feature_image { get; set; }
    public string visibility { get; set; }
    public string codeinjection_head { get; set; }
    public object codeinjection_foot { get; set; }
    public object canonical_url { get; set; }
    public string accent_color { get; set; }
    public string url { get; set; }
  }
  #endregion
}
