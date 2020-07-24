
using System;
using System.Collections.Generic;

namespace Speckle.Core.Api.GqlModels
{
  public class StreamCreateInput
  {
    public string name { get; set; }
    public string description { get; set; }
    public bool isPublic { get; set; }
  }

  public class StreamUpdateInput
  {
    public string id { get; set; }
    public string name { get; set; }
    public string description { get; set; }
    public bool isPublic { get; set; }
  }

  public class Stream
  {
    public string id { get; set; }
    public string name { get; set; }
    public string description { get; set; }

    public bool isPublic { get; set; }

    public string createdAt { get; set; }
    public string updatedAt { get; set; }

    public List<Collaborator> collaborators { get; set; }
    public Branches branches { get; set; }
  }

  public class Collaborator
  {
    public string id { get; set; }
    public string name { get; set; }
    public string role { get; set; }
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
    public List<object> items { get; set; }

  }

  public class Branch
  {
    public string id { get; set; }
    public string name { get; set; }
    public string description { get; set; }
    public Commits commits { get; set; }

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
    public string username { get; set; }
    public string email { get; set; }
    public string name { get; set; }
    public string bio { get; set; }
    public string company { get; set; }
    public string avatar { get; set; }
    public bool verified { get; set; }
    //public object profiles { get; set; }
    public string role { get; set; }
    public Streams streams { get; set; }

  }

  public class UserData
  {
    public User user { get; set; }

  }

  public class StreamData
  {
    public Stream stream { get; set; }

  }
}