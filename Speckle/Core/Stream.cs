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

    public List<string> Commits { get; set; } = new List<string>();

    [JsonIgnore]
    public Commit CurrentCommit { get; set; }

    public List<Branch> Branches { get; set; } = new List<Branch>();

    public Branch CurrentBranch { get; set; }

    public List<Tag> Tags { get; set; } = new List<Tag>();

    public List<Remote> Remotes = new List<Remote>();

    public Stream()
    {
      LocalObjectTransport = new DiskTransport();
      LocalStreamTransport = new DiskTransport(scope: "Streams", splitPath: false);
      Serializer = new Serializer();
    }

    #region Operation 

    public void Add(IEnumerable<Base> objects)
    {
      if (CurrentCommit == null) CurrentCommit = new Commit();

      if (Commits.Count != 0) CurrentCommit.previousCommit = Commits[Commits.Count - 1];

      int i = 0, objCount = objects.Count();
      foreach (var obj in objects)
      {
        Serializer.SerializeAndSave(obj, LocalObjectTransport);
        CurrentCommit.objects.Add(obj.hash);
        EmitOnProgress(++i, objCount, "Adding objects");
      }
    }

    public void Remove(IEnumerable<Base> objects)
    {
      if (CurrentCommit == null) CurrentCommit = new Commit();

      if (Commits.Count != 0) CurrentCommit.previousCommit = Commits[Commits.Count - 1];

      int i = 0, objCount = objects.Count();
      foreach (var obj in objects)
      {
        CurrentCommit.objects.Remove(obj.hash);
        EmitOnProgress(++i, objCount, "Removing objects");
      }
    }

    public void SetState(IEnumerable<Base> objects)
    {
      if (CurrentCommit == null) CurrentCommit = new Commit();

      if (Commits.Count != 0) CurrentCommit.previousCommit = Commits[Commits.Count - 1];

      int i = 0, objCount = objects.Count();
      foreach (var obj in objects)
      {
        Serializer.SerializeAndSave(obj, LocalObjectTransport);
        CurrentCommit.objects.Add(obj.hash);
        EmitOnProgress(++i, objCount, "Adding objects");
      }
    }

    public void Commit(string message)
    {
      if (CurrentBranch == null && Branches.Count == 0 )
      {
        Branches.Add(new Branch() { name = "master" });
        CurrentBranch = Branches[0];
      }

      CurrentCommit.description = message;

      Serializer.SerializeAndSave(CurrentCommit, LocalObjectTransport);
      EmitOnProgress(1, 2, "Comitting revision");

      Commits.Add(CurrentCommit.hash);

      CurrentBranch.head = CurrentCommit.hash;

      var result = JsonConvert.SerializeObject(this);

      LocalStreamTransport.SaveObject(this.Id, result, true);
      EmitOnProgress(2, 2, "Comitting revision");
    }

    public void Branch(string branchName)
    {
      //TODO: check unique
      Branches.Add(new Branch() { name = branchName, head = CurrentCommit.hash });
    }

    public void Tag(string tagName, string commitHash)
    {
      //TODO
    }

    public void Checkout(string commit, Remote remote = null)
    {

    }

    public void Checkout(Tag tag, Remote remote = null)
    {

    }

    public void Checkout(Branch branch, Remote remote = null)
    {
      if(remote == null)
      {
        CurrentCommit = (Commit)Serializer.Deserialize(LocalObjectTransport.GetObject(branch.head));
        return;
      }
    }

    public void Push(Remote remote, Branch branch = null)
    {
      if (branch == null && CurrentBranch != null)
        branch = CurrentBranch;
      else
        throw new Exception("No current branch to push to remote. Nothing to commit!");

      //TODO: push things
    }

    #endregion

    #region Loading

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

      var stream = JsonConvert.DeserializeObject<Stream>(LocalStreamTransport.GetObject(id));

      // Reinstantiate the current revision if it exists.
      if (stream.CurrentBranch != null)
      {
        stream.Checkout(stream.CurrentBranch);
      }

      return stream;
    }

    public static Stream Load(Remote remote)
    {

      return null;
    }

    #endregion

    #region progress events

    protected virtual void EmitOnProgress(int current, int total, string scope)
    {
      OnProgress?.Invoke(this, new ProgressEventArgs(current, total, scope));
    }

    public event EventHandler<ProgressEventArgs> OnProgress;

    #endregion
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
    public Account Account { get; set; }
    public List<Commit> RemoteRevisions { get; set; }
    public Stream Stream { get; set; }


    public Remote() { }

    public Remote(Account account, string StreamId)
    {

    }

    public void Refresh() { }


  }

  public class Commit : Base
  {
    public List<string> objects { get; set; } = new List<string>();

    [ExcludeHashing]
    public string name { get; set; }

    [ExcludeHashing]
    public string description { get; set; }

    [ExcludeHashing]
    public string previousCommit { get; set; }

    [ExcludeHashing]
    public User Author { get; set; }

    [ExcludeHashing]
    public string CreatedOn { get; } = DateTime.UtcNow.ToString("o");

    [ExcludeHashing]
    public Branch Branch { get; }

    public Commit() { }
  }

  public class Tag
  {
    public string name { get; set; }
    public string commit { get; set; }
  }

  public class Branch
  {
    public string name { get; set; }
    public string head { get; set; }
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
