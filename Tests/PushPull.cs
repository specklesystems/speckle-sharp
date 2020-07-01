using System.Collections.Generic;
using Speckle.Transports;
using NUnit.Framework;
using Speckle.Models;
using Speckle.Core;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;

namespace Tests
{
  [TestFixture]
  public class PushPull
  {
    string commitId_01, commitId_02;
    int numObjects = 3001;


    [Test(Description = "Pushing a commit locally"), Order(1)]
    public void PushCommitLocal()
    {
      var commit = new Commit();
      var rand = new Random();

      for (int i = 0; i < numObjects; i++)
      {
        commit.Objects.Add(new Point(i, i, i + rand.NextDouble()) { applicationId = i + "-___/---" });
      }

      commitId_01 = Operations.Push(commit).Result;

      Assert.NotNull(commitId_01);
      TestContext.Out.WriteLine($"Written {numObjects + 1} objects. Commit id is {commitId_01}");

    }

    [Test(Description = "Pulling a commit locally"), Order(2)]
    public void PullCommitLocal()
    {
      var commitPulled = (Commit)Operations.Pull(commitId_01).Result;

      Assert.AreEqual(commitPulled.GetType(), typeof(Commit));
      Assert.AreEqual(commitPulled.Objects.Count, numObjects);
    }

    [Test(Description = "Pushing and pulling a commit locally"), Order(3)]
    public async Task PushPullSmallCommitLocal()
    {
      var commit = new Commit();
      var rand = new Random();

      for (int i = 0; i < 30; i++)
      {
        commit.Objects.Add(new Point(i, i, i + rand.NextDouble()) { applicationId = i + "-ugh/---" });
      }

      commitId_01 = await Operations.Push(commit);

      Assert.NotNull(commitId_01);
      TestContext.Out.WriteLine($"Written {numObjects + 1} objects. Commit id is {commitId_01}");

      var commitPulled = (Commit)await Operations.Pull(commitId_01);

      Assert.AreEqual(commitPulled.GetType(), typeof(Commit));
      Assert.AreEqual(commitPulled.Objects.Count, 30);
    }

    [Test(Description = "Should show progress!"), Order(4)]
    public async Task PushProgressReports()
    {
      var commit = new Commit();
      var rand = new Random();

      for (int i = 0; i < 30; i++)
      {
        commit.Objects.Add(new Point(i, i, i + rand.NextDouble()) { applicationId = i + "-fab/---" });
      }

      ConcurrentDictionary<string, int> progress = null;
      commitId_02 = await Operations.Push(commit, onProgressAction: (dict) =>
      {
        progress = dict;
      }); 

      Assert.NotNull(progress);
      Assert.Contains("Serialization", progress.Keys.ToArray());
    }

    [Test(Description = "Should show progress!"), Order(5)]
    public async Task PullProgressReports()
    {
      ConcurrentDictionary<string, int> progress = null;
      var pulledCommit = await Operations.Pull(commitId_02, onProgressAction: (dict)=>
      {
        progress = dict;
      });
      Assert.NotNull(progress);
      Assert.Contains("Deserialization", progress.Keys.ToArray());
    }


  }
}