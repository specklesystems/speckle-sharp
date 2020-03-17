using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Speckle.Models;
using Speckle.Serialisation;
using Speckle.Transports;

namespace Speckle.Core
{

  public partial class Stream
  {
    [JsonIgnore]
    public readonly ITransport LocalObjectTransport;

    [JsonIgnore]
    public readonly ITransport LocalStreamTransport;

    [JsonIgnore]
    public readonly Serializer Serializer;

    public string Id { get; set; } = Guid.NewGuid().ToString().ToLower();

    public string Name { get; set; } = "Unnamed Stream";

    [JsonIgnore]
    public Commit CurrentCommit { get; set; }

    public string PreviousCommitId { get; set; }

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

    /// <summary>
    /// Initializes a bare repository with a default "master" branch and current commit.
    /// </summary>
    void Initialize()
    {
      // set up master branch
      var branch = new Branch("master");
      CurrentBranch = "master";
      DefaultBranch = "master";
      Branches.Add(branch);

      // set up an empty staging commit
      CurrentCommit = new Commit();
    }

    #region Local Operation 

    /// <summary>
    /// Adds objects to the current commit.
    /// </summary>
    /// <param name="objects"></param>
    public void Add(IEnumerable<Base> objects)
    {
      if (CurrentCommit == null)
      {
        Initialize();
      }

      CurrentCommit.Objects.AddRange(objects);
    }

    /// <summary>
    /// Removes objects from the current commit.
    /// </summary>
    /// <param name="objects"></param>
    public void Remove(IEnumerable<Base> objects)
    {
      if (CurrentCommit == null)
      {
        throw new Exception("No objects present in current commit.");
      }

      CurrentCommit.Objects.RemoveAll(obj => objects.Contains(obj)); // TODO: this probably relies on GetHashCode, check if it actually does the correct thing
    }

    /// <summary>
    /// Flushes the current commit and sets the provided objects as its state.
    /// </summary>
    /// <param name="objects"></param>
    public void SetState(IEnumerable<Base> objects)
    {
      if (CurrentCommit == null)
      {
        Initialize();
      }

      CurrentCommit.Objects = objects.ToList();
    }

    /// <summary>
    /// Persists the current state as a commit in this model's history.
    /// </summary>
    /// <param name="message"></param>
    public void Commit(string message)
    {
      if (CurrentBranch == null)
      {
        throw new Exception("No current branch set.");
      }

      CurrentCommit.Description = message;

      var currentBranch = GetCurrentBranch();

      // Setup the commit chain
      if (PreviousCommitId != null)
      {
        CurrentCommit.Parents = new HashSet<string>() { PreviousCommitId };
      }

      var total = CurrentCommit.Objects.Count + 1; // Total object count needs to include the parent commit object.
      var currentCount = 0;

      Serializer.SerializeAndSave(CurrentCommit, LocalObjectTransport, (string scope) => EmitOnProgress(++currentCount, total, scope));

      EmitOnProgress(1, 2, "Comitting revision");

      PreviousCommitId = CurrentCommit.hash;
      GetCurrentBranch().Commits.Add(PreviousCommitId);

      var result = JsonConvert.SerializeObject(this);

      LocalStreamTransport.SaveObject(this.Id, result, true);
      EmitOnProgress(2, 2, "Comitting revision");
    }

    /// <summary>
    /// Creates a new branch in this model.
    /// </summary>
    /// <param name="branchName">The name of the branch. Needs to be unique.</param>
    public void Branch(string branchName)
    {
      var unique = Branches.FirstOrDefault(br => br.Name == branchName) == null;

      if (!unique)
        throw new Exception($"A branch called {branchName} already exits.");

      Branches.Add(new Branch() { Name = branchName });
      CurrentBranch = branchName;
    }

    /// <summary>
    /// Creates a new tag at the specified commit.
    /// </summary>
    /// <param name="tagName"></param>
    /// <param name="commitHash"></param>
    public void Tag(string tagName, string commitHash)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// The current state 
    /// </summary>
    /// <param name="branch"></param>
    /// <param name="commit"></param>
    public void Checkout(string branchName, string commit = null)
    {
      var branch = Branches.First(br => br.Name == branchName);

      commit = commit == null ? branch.Head : commit;

      EmitOnProgress(1, 1, "Checking out commit");

      var currentCount = 0;
      var commitString = LocalObjectTransport.GetObject(commit);
      var total = ((JArray)JObject.Parse(commitString).GetValue("Objects")).Count + 1;

      CurrentCommit = (Commit)Serializer.DeserializeAndGet(commitString, LocalObjectTransport, (string scope) => EmitOnProgress(++currentCount, total, scope));

      CurrentBranch = branch.Name;

      if (branch.Commits.Count > 1)
      {
        PreviousCommitId = branch.Commits[branch.Commits.Count - 2];
      }
    }

    #endregion

    #region Remote Operations

    public void AddRemote(Remote remote)
    {
      remote.LocalStream = this;
      Remotes.Add(remote); // TODO: Check uniqueness
    }

    public void RemoveRemote(string remoteName)
    {
      throw new NotImplementedException();
    }

    public void Push(string remoteName, string branchName, string commit = null, bool preserveHistory = true)
    {
      var remote = Remotes.First(r => r.Name == remoteName);
      remote.Push(branchName, commit, preserveHistory, OnProgress);
    }

    public void Pull()
    {

    }

    #endregion

    #region Loading: local and remote

    /// <summary>
    /// Loads a local stream.
    /// </summary>
    /// <param name="streamId"></param>
    /// <param name="LocalObjectTransport"></param>
    /// <param name="LocalStreamTransport"></param>
    /// <param name="OnProgress"></param>
    /// <returns></returns>
    public static Stream Load(string streamId, ITransport LocalObjectTransport = null, ITransport LocalStreamTransport = null, EventHandler<ProgressEventArgs> OnProgress = null)
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

      if (OnProgress != null)
      {
        OnProgress.Invoke(stream, new ProgressEventArgs(1, 1, "Loaded stream"));
        stream.OnProgress += OnProgress;
      }

      // Reinstantiate the current commit, if it exists, from the current branch.
      if (stream.CurrentBranch != null)
      {
        stream.Checkout(stream.GetCurrentBranch().Name);
      }
      else
      {
        stream.Checkout(stream.GetDefaultBranch().Name);
      }

      return stream;
    }

    /// <summary>
    /// Loads a stream from a remote.
    /// </summary>
    /// <param name="remote"></param>
    /// <returns></returns>
    public static Stream Load(Remote remote)
    {
      throw new NotImplementedException();
    }

    #endregion

    #region Progress events

    public event EventHandler<ProgressEventArgs> OnProgress;

    protected virtual void EmitOnProgress(int current, int total, string scope)
    {
      OnProgress?.Invoke(this, new ProgressEventArgs(current, total, scope));
    }

    #endregion

    #region Branch convenience methods

    public Branch GetCurrentBranch()
    {
      return this.Branches.Find(br => br.Name == this.CurrentBranch);
    }

    public Branch GetDefaultBranch()
    {
      return this.Branches.Find(br => br.Name == this.DefaultBranch);
    }

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

  public class Tag
  {
    public string name { get; set; }
    public string commit { get; set; }
  }

  public class Branch
  {
    public string Name { get; set; }

    public string Head { get => Commits.Count > 0 ? Commits[Commits.Count - 1] : null; }

    public List<string> Commits { get; set; } = new List<string>();

    public Branch() { }

    public Branch(string name)
    {
      this.Name = name;
    }
  }

}
