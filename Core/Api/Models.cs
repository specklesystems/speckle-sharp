
using System;
using System.Collections.Generic;

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

  public class StreamGrantPermissionInput
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

  #endregion

  public class  Stream
  {
    public string id { get; set; }
    public string name { get; set; }
    public string description { get; set; }

    public bool isPublic { get; set; }

    public string createdAt { get; set; }
    public string updatedAt { get; set; }

    public List<Collaborator> collaborators { get; set; }
    public Branches branches { get; set; }

    public override string ToString()
    {
      return $"Steam ({name} | {id})";
    }
  }

  public class Collaborator
  {
    public string id { get; set; }
    public string name { get; set; }
    public string role { get; set; }

    public override string ToString()
    {
      return $"Collaborator ({name} | {role} | {id})";
    }
  }

  public class Branches
  {
    public int totalCount { get; set; }
    public DateTime cursor { get; set; }
    public List<Branch> items { get; set; }
  }

  public class Commits
  {
    public int totalCount { get; set; }
    public object cursor { get; set; }
    public List<Commit> items { get; set; }

  }

  public class Commit
  {
    public string id { get; set; }
    public string message { get; set; }
    public string authorName { get; set; }
    public string authorId { get; set; }
    public string createdAt { get; set; }

    public string referencedObject { get; set; }

    public override string ToString()
    {
      return $"Commit ({message} | {id})";
    }

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
    public DateTime cursor { get; set; }
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

    public override string ToString()
    {
      return $"User ({email} | {name} | {id})";
    }

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
    public DateTime cursor { get; set; }
    public List<User> items { get; set; }
  }

  public class StreamData
  {
    public Stream stream { get; set; }

  }

  public class StreamsData
  {
    public  Streams streams { get; set; }
  }
}
