using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Speckle.Models;
using Speckle.Serialisation;
using Speckle.Transports;

namespace Speckle.Core
{
  public class Stream
  {
    DiskTransport LocalObjectTransport;
    DiskTransport LocalStreamTransport;
    Serializer Serializer;

    public string Id { get; set; } = Guid.NewGuid().ToString().ToLower();

    public string Name { get; set; }

    public List<string> Revisions { get; set; } = new List<string>();

    public Revision CurrentRevision { get; set; }

    public List<Remote> Remotes = new List<Remote>();

    public Stream()
    {
      LocalObjectTransport = new DiskTransport();
      LocalStreamTransport = new DiskTransport(scope: "Streams", splitPath: false);
      Serializer = new Serializer();
    }

    public void Add(IEnumerable<Base> objects)
    {
      throw new NotImplementedException();
    }

    public void Remove(IEnumerable<Base> objects)
    {
      throw new NotImplementedException();
    }

    public void SetState(IEnumerable<Base> objects)
    {
      var revision = new Revision();

      if (Revisions.Count != 0)
      {
        revision.previousRevisionHash = Revisions[Revisions.Count - 1];
      }

      foreach (var obj in objects)
      {
        Serializer.SerializeAndSave(obj, LocalObjectTransport);
        revision.objects.Add(obj.hash);
      }

      CurrentRevision = revision;
    }

    public void Commit(string message)
    {
      //TODO: Write the revision to disk
      Revisions[Revisions.Count - 1].description = message;

      Serializer.SerializeAndSave(Revisions, LocalObjectTransport);

      var result = JsonConvert.SerializeObject(this);
      var test = result;
      //TODO: Write the stream to disk
      //throw new NotImplementedException();
    }

    //public static string ToJson(this Stream stream)
    //{
    //  return null;
    //}

  }

  public class Remote
  {
    public Account Account;
    public List<Revision> Revisions;

    public Remote() { }

    public void Push(Revision rev)
    {
      throw new NotImplementedException();
    }

    public void Fetch()
    {
      throw new NotImplementedException();
    }

    public void Pull()
    {
      throw new NotImplementedException();
    }
  }

  public class Revision : Base
  {
    public List<string> objects { get; set; } = new List<string>();

    [ExcludeHashing]
    public string name { get; set; }

    [ExcludeHashing]
    public string description { get; set; }

    [ExcludeHashing]
    public List<string> tags { get; set; } = new List<string>();

    [ExcludeHashing]
    public string previousRevisionHash { get; set; }

    [ExcludeHashing]
    public User Author { get; set; }

    [ExcludeHashing]
    public string CreatedOn { get; } = DateTime.UtcNow.ToString("o");

    public Revision() { }
  }

  public class Account
  {
    public string Email { get; set; }
    public string ServerName { get; set; }
    public string ServerUrl { get; set; }
    public string ApiToken { get; set; }

    public Account() { }
  }

  public class User
  {
    public string Email { get; set; }
    public string Name { get; set; }

    public User() { }
  }


}
