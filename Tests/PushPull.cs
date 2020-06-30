using System.Collections.Generic;
using Speckle.Transports;
using NUnit.Framework;
using Speckle.Models;
using Speckle.Core;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace Tests
{
  [TestFixture]
  public class PushPull
  {
    string commitId;
    int numObjects = 3_000;


    [Test(Description = "Pushing a commit locally"), Order(1)]
    public void PushAndPullCommitLocal()
    {
      var commit = new Commit();
      var rand = new Random();

      for (int i = 0; i < numObjects; i++)
      {
        commit.Objects.Add(new Point(i, i, i) { applicationId = i + "-id" });
      }

      commitId = Operations.Push(commit).Result;

      Assert.NotNull(commitId);
      TestContext.Out.WriteLine($"Written {numObjects + 1} objects. Commit id is {commitId}");

      var commitPulled = (Commit)Operations.Pull(commitId).Result;

      Assert.AreEqual(commitPulled.GetType(), typeof(Commit));
      Assert.AreEqual(commitPulled.Objects.Count, numObjects);
      return;
    }

    //TODO: add
  }
}