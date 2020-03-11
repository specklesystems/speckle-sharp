using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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

    //public List<string> Commits { get; set; } = new List<string>();

    [JsonIgnore]
    public Commit CurrentCommit { get; set; }

    public List<Branch> Branches { get; set; } = new List<Branch>();

    public string DefaultBranch { get; set; }

    public string CurrentBranch { get; set; }

    public List<Tag> Tags { get; set; } = new List<Tag>();

    public List<Remote> Remotes = new List<Remote>();

    public Stream()
    {
      LocalObjectTransport = new DiskTransport();
      LocalStreamTransport = new DiskTransport(scope: "Streams", splitPath: false);
      Serializer = new Serializer();
    }

    public void Initialize()
    {
      // set up master branch
      var branch = new Branch("master");
      CurrentBranch = "master";
      DefaultBranch = "master";
      Branches.Add(branch);

      // set up an empty staging commit
      CurrentCommit = new Commit();
    }

    #region Operation 

    public void Add(IEnumerable<Base> objects)
    {
      if (CurrentCommit == null)
      {
        Initialize();
      }

      //if (CurrentBranch.Commits.Count != 0) CurrentCommit.previousCommit = CurrentBranch.Commits[CurrentBranch.Commits.Count - 1];

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
      if (CurrentCommit == null)
      {
        Initialize();
      }

      //if (CurrentBranch.Commits.Count != 0) CurrentCommit.previousCommit = CurrentBranch.Commits[CurrentBranch.Commits.Count - 1];

      int i = 0, objCount = objects.Count();
      foreach (var obj in objects)
      {
        CurrentCommit.objects.Remove(obj.hash);
        EmitOnProgress(++i, objCount, "Removing objects");
      }
    }

    public void SetState(IEnumerable<Base> objects)
    {
      if (CurrentCommit == null)
      {
        Initialize();
      }

      //if (CurrentBranch.Commits.Count != 0) CurrentCommit.previousCommit = CurrentBranch.Commits[CurrentBranch.Commits.Count - 1];

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
      if (CurrentBranch == null)
      {
        throw new Exception("No current branch set.");
      }

      CurrentCommit.description = message;
      if (GetCurrentBranch().Commits.Count != 0)
      {
        CurrentCommit.previousCommit = GetCurrentBranch().Commits[GetCurrentBranch().Commits.Count - 1];
      }


      Serializer.SerializeAndSave(CurrentCommit, LocalObjectTransport);
      EmitOnProgress(1, 2, "Comitting revision");

      GetCurrentBranch().Commits.Add(CurrentCommit.hash);

      var result = JsonConvert.SerializeObject(this);

      LocalStreamTransport.SaveObject(this.Id, result, true);
      EmitOnProgress(2, 2, "Comitting revision");
    }

    public void Branch(string branchName)
    {
      //TODO: check unique
      Branches.Add(new Branch() { name = branchName });
      CurrentBranch = branchName;
    }

    public void Tag(string tagName, string commitHash)
    {
      //TODO
    }

    public void Checkout(Branch branch, string commit = null, Remote remote = null)
    {
      if (remote == null)
      {
        CurrentCommit = (Commit)Serializer.Deserialize(LocalObjectTransport.GetObject(commit == null ? branch.head : commit));
        CurrentBranch = branch.name;
        return;
      }
      else
      {
        // TODO: Pull from remote
      }
    }

    public void Push(Remote remote, Branch branch = null)
    {
      if (branch == null && CurrentBranch != null)
        branch = GetCurrentBranch();
      else
        throw new Exception("No current branch to push to remote. Nothing to commit!");

      //TODO: push things
      //Handover to remote for functionality... I guess.
    }

    #endregion

    public Branch GetCurrentBranch()
    {
      return this.Branches.Find(br => br.name == this.CurrentBranch);
    }

    public Branch GetDefaultBranch()
    {
      return this.Branches.Find(br => br.name == this.DefaultBranch);
    }

    #region Loading

    public static Stream Load(string streamId, ITransport LocalObjectTransport = null, ITransport LocalStreamTransport = null)
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

      var stream = JsonConvert.DeserializeObject<Stream>(LocalStreamTransport.GetObject(streamId));

      // Reinstantiate the current commit, if it exists, from the current branch.
      if (stream.CurrentBranch != null)
      {
        stream.Checkout(stream.GetCurrentBranch());
      }
      else
      {
        stream.Checkout(stream.GetDefaultBranch());
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

    //[ExcludeHashing]
    //public Branch Branch { get; }

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
    public string head { get => Commits[0]; }
    public List<string> Commits { get; set; } = new List<string>();

    public Branch() { }

    public Branch(string name)
    {
      this.name = name;
    }
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
