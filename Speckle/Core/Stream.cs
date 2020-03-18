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
    public ITransport LocalObjectTransport { get; set; }

    [JsonIgnore]
    public ITransport LocalStreamTransport { get; set; }

    [JsonIgnore]
    public readonly Serializer Serializer;

    public string Id { get; set; } = Guid.NewGuid().ToString().ToLower();

    public string Name { get; set; } = "Unnamed Stream";

    [JsonIgnore]
    public Commit CurrentCommit { get; set; }

    public string PreviousCommitId { get; set; }

    public List<Branch> Branches { get; set; } = new List<Branch>();

    public string DefaultBranch { get; set; } = "master";

    public string CurrentBranch { get; set; } = "master";

    //public string CurrentBranch { get; set; }

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
      DefaultBranch = "master";
      CurrentBranch = "master";
      Branches.Add(branch);

      // set up an empty staging commit
      CurrentCommit = new Commit();
    }

    #region Staging & Comitting

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
    /// <param name="message">A short message describing this commit.</param>
    /// <param name="branchName">The name of the branch you want this commit associated with.</param>
    public void Commit(string message = "draft commit", string branchName = null)
    {
      // If no branch name is provided, default to the current branch.
      if (branchName == null) branchName = CurrentBranch;

      // Check if that branch exists!
      var branch = Branches.FirstOrDefault(br => br.Name == branchName);

      // If it doesn't, create it.
      if (branch == null)
      {
        branch = CreateBranch(branchName);
        // ... and set it as the current branch.
        CurrentBranch = branchName;
      }

      CurrentCommit.Description = message;

      // Setup the commit chain.
      if (PreviousCommitId != null)
      {
        CurrentCommit.Parents = new HashSet<string>() { PreviousCommitId };
      }

      var total = CurrentCommit.Objects.Count + 1; // Total object count needs to include the parent commit object.
      var currentCount = 0;

      Serializer.SerializeAndSave(CurrentCommit, LocalObjectTransport, (string scope) => EmitOnProgress(++currentCount, total, scope));

      EmitOnProgress(1, 2, "Comitting revision");

      PreviousCommitId = CurrentCommit.hash;

      branch.Commits.Add(PreviousCommitId);

      // Save the stream locally
      LocalStreamTransport.SaveObject(this.Id, JsonConvert.SerializeObject(this), true);
      EmitOnProgress(2, 2, "Comitting revision");
    }

    /// <summary>
    /// Creates a new branch in this model.
    /// </summary>
    /// <param name="branchName">The name of the branch. Needs to be unique.</param>
    public Branch CreateBranch(string branchName)
    {
      var unique = Branches.FirstOrDefault(br => br.Name == branchName) == null;

      if (!unique)
        throw new Exception($"A branch called {branchName} already exits.");

      var branch = new Branch(branchName);
      Branches.Add(branch);

      return branch;
    }

    /// <summary>
    /// Creates a new tag at the specified commit.
    /// </summary>
    /// <param name="tagName"></param>
    /// <param name="commitHash"></param>
    public void CreateTag(string tagName, string commitHash)
    {
      var found = Tags.FindIndex(t => t.name == tagName) != -1;

      if (found) throw new Exception($"Tag {tagName} already exists.");

      found = GetAllCommits().IndexOf(commitHash) != -1;

      if (!found) throw new Exception($"Commit {commitHash} does not exist.");

      var tag = new Tag() { name = tagName, commit = commitHash };
      Tags.Add(tag);
    }

    /// <summary>
    /// The current state 
    /// </summary>
    /// <param name="branch"></param>
    /// <param name="commit"></param>
    public void Checkout(string branchName, string commit = null)
    {
      // Find that branch; will throw an error if not found.
      var branch = Branches.FirstOrDefault(br => br.Name == branchName);

      if (branch == null) throw new Exception($"Branch {branchName} does not exist.");

      // If no commit is specified, default to the branch's latest (head).
      commit = commit == null ? branch.Head : commit;

      // Defenisve check on wether the commit is part of the selected branch or not.
      if (branch.Commits.IndexOf(commit) == -1) throw new Exception($"Commit {commit} does not exist in branch {branchName}.");

      EmitOnProgress(1, 1, "Checking out commit");

      var currentCount = 0;
      var commitString = LocalObjectTransport.GetObject(commit);
      var total = ((JArray)JObject.Parse(commitString).GetValue("Objects")).Count + 1; // Hacky, can be deserialized to a ShallowCommit

      CurrentCommit = (Commit)Serializer.DeserializeAndGet(commitString, LocalObjectTransport, (string scope) => EmitOnProgress(++currentCount, total, scope));

      // Set the previous commit if our branch has more than one commit present.
      if (branch.Commits.Count > 1)
      {
        PreviousCommitId = branch.Commits[branch.Commits.Count - 2];
      }

      // Set the current branch name for subsequent ergonomic calls to Commit()
      CurrentBranch = branchName;
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
      var idx = Remotes.FindIndex(r => r.Name == remoteName);
      if (idx == -1) throw new Exception($"Remote {remoteName} does not exist in this stream.");
      Remotes.RemoveAt(idx);
    }

    public void Publish(string remoteName, string branchName = null, string commit = null, bool preserveHistory = false)
    {
      var remote = Remotes.FirstOrDefault(r => r.Name == remoteName);

      if (remote == null) throw new Exception($"Remote {remoteName} could not be found.");

      // Set defaults if not provided explicitely:

      // Fallback to current branch
      if (branchName == null) branchName = CurrentBranch;

      // Fallback to its head
      if (commit == null) commit = GetCurrentBranch().Head;

      // Finally, tell the remote to push and pass on the event handler.
      remote.Push(branchName, commit, preserveHistory, OnProgress);
    }

    public void Pull()
    {
      // get from remote
      // then checkout based on arguments
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
    public static Stream Load(string streamId, string branchName = null, ITransport LocalObjectTransport = null, ITransport LocalStreamTransport = null, EventHandler<ProgressEventArgs> OnProgress = null)
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

      // Set the transports in case they were not null originally
      stream.LocalObjectTransport = LocalObjectTransport;
      stream.LocalStreamTransport = LocalStreamTransport;

      if (OnProgress != null)
      {
        OnProgress.Invoke(stream, new ProgressEventArgs(1, 1, "Loaded stream"));
        stream.OnProgress += OnProgress;
      }

      // Reinstantiate the current commit, if it exists, from the current branch.
      if (branchName == null)
      {
        stream.Checkout(stream.DefaultBranch);
      }
      else
      {
        stream.Checkout(branchName);
      }

      // Set the remote's reference to the local stream.
      stream.Remotes.ForEach(rem => rem.LocalStream = stream);

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

    #region Convenience methods

    public Branch GetDefaultBranch()
    {
      return Branches.Find(br => br.Name == this.DefaultBranch);
    }

    public Branch GetCurrentBranch()
    {
      return Branches.Find(br => br.Name == this.CurrentBranch);
    }

    public List<string> GetAllCommits()
    {
      var allCommits = new List<string>();
      foreach (var b in Branches)
        allCommits.AddRange(b.Commits);

      return allCommits;
    }

    public Branch GetCommitBranch(string commitId)
    {
      return Branches.FirstOrDefault(br => br.Commits.Contains(commitId));
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
