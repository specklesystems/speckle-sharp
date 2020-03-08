using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Speckle.Models;
using Speckle.Serialisation;
using Speckle.Transports;

namespace Speckle.Core
{
  public class Stream
  {
    ITransport LocalObjectTransport;
    ITransport LocalStreamTransport;
    Serializer Serializer;

    public string Id { get; set; } = Guid.NewGuid().ToString().ToLower();

    public string Name { get; set; } = "Unnamed Stream";

    public List<string> Revisions { get; set; } = new List<string>();

    [JsonIgnore]
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
      if (CurrentRevision == null) CurrentRevision = new Revision();

      if (Revisions.Count != 0) CurrentRevision.previousRevisionHash = Revisions[Revisions.Count - 1];

      int i = 0, objCount = objects.Count();
      foreach (var obj in objects)
      {
        Serializer.SerializeAndSave(obj, LocalObjectTransport);
        CurrentRevision.objects.Add(obj.hash);
        EmitOnProgress(++i, objCount, "Adding objects");
      }
    }

    public void Remove(IEnumerable<Base> objects)
    {
      if (CurrentRevision == null) CurrentRevision = new Revision();

      if (Revisions.Count != 0) CurrentRevision.previousRevisionHash = Revisions[Revisions.Count - 1];

      int i = 0, objCount = objects.Count();
      foreach (var obj in objects)
      {
        CurrentRevision.objects.Remove(obj.hash);
        EmitOnProgress(++i, objCount, "Removing objects");
      }
    }

    public void SetState(IEnumerable<Base> objects)
    {
      if (CurrentRevision == null) CurrentRevision = new Revision();

      if (Revisions.Count != 0) CurrentRevision.previousRevisionHash = Revisions[Revisions.Count - 1];

      int i = 0, objCount = objects.Count();
      foreach (var obj in objects)
      {
        Serializer.SerializeAndSave(obj, LocalObjectTransport);
        CurrentRevision.objects.Add(obj.hash);
        EmitOnProgress(++i, objCount, "Adding objects");
      }
    }

    public void Commit(string message)
    {
      CurrentRevision.description = message;

      Serializer.SerializeAndSave(CurrentRevision, LocalObjectTransport);
      EmitOnProgress(1, 2, "Comitting revision");

      Revisions.Add(CurrentRevision.hash);

      var result = JsonConvert.SerializeObject(this);

      LocalStreamTransport.SaveObject(this.Id, result, true);
      EmitOnProgress(2, 2, "Comitting revision");
    }

    protected virtual void EmitOnProgress(int current, int total, string scope)
    {
      OnProgress?.Invoke(this, new ProgressEventArgs(current, total, scope));
    }

    public event EventHandler<ProgressEventArgs> OnProgress;

    public static Stream Load(string id, ITransport LocalObjectTransport = null, ITransport LocalStreamTransport = null)
    {
      if (LocalObjectTransport == null)
      {
        LocalObjectTransport = new DiskTransport();
      }

      if (LocalStreamTransport == null)
      {
        LocalStreamTransport = new DiskTransport(scope: "Streams", splitPath: false);
      }

      var Serializer = new Serializer();

      var @string = LocalStreamTransport.GetObject(id);
      var stream = JsonConvert.DeserializeObject<Stream>(@string);

      // Reinstantiate the current revision if it exists.
      if (stream.Revisions.Count > 0)
      {
        stream.CurrentRevision = (Revision)Serializer.Deserialize(LocalObjectTransport.GetObject(stream.Revisions[stream.Revisions.Count - 1]));
      }

      return stream;
    }
  }

  public class ProgressEventArgs : EventArgs
  {
    public int current { get; set; }
    public int total { get; set; }
    public string scope { get; set; }
    public ProgressEventArgs(int current, int total, string scope)
    {
      this.current = current; this.total = total; this.scope = scope;
    }
  }

  public class Remote
  {
    public Account Account;
    public List<Revision> Revisions;

    public Remote() { }

    public void Push(Revision rev) // Upload a revision
    {
      throw new NotImplementedException();
    }

    public void Fetch() // Get stream's revisions list
    {
      throw new NotImplementedException();
    }

    public void Pull() // 
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
