using System.Collections.Generic;
using Speckle.Core.Transports;
using NUnit.Framework;
using Speckle.Core.Models;
using Speckle.Core.Api;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;

namespace Tests
{
  [TestFixture]
  public class UploadDownload
  {
    string commitId_01, commitId_02;
    int numObjects = 3001;


    [Test(Description = "Pushing a commit locally"), Order(1)]
    public void LocalUpload()
    {
      var commit = new Commit();
      var rand = new Random();

      for (int i = 0; i < numObjects; i++)
      {
        commit.Objects.Add(new Point(i, i, i + rand.NextDouble()) { applicationId = i + "-___/---" });
      }

      commitId_01 = Operations.Upload(commit).Result;

      Assert.NotNull(commitId_01);
      TestContext.Out.WriteLine($"Written {numObjects + 1} objects. Commit id is {commitId_01}");

    }

    [Test(Description = "Pulling a commit locally"), Order(2)]
    public void PullCommitLocal()
    {
      var commitPulled = (Commit)Operations.Download(commitId_01).Result;

      Assert.AreEqual(commitPulled.GetType(), typeof(Commit));
      Assert.AreEqual(commitPulled.Objects.Count, numObjects);
    }

    [Test(Description = "Pushing and pulling a commit locally"), Order(3)]
    public async Task LocalUploadDownloadSmall()
    {
      var commit = new Commit();
      var rand = new Random();

      for (int i = 0; i < 30; i++)
      {
        commit.Objects.Add(new Point(i, i, i + rand.NextDouble()) { applicationId = i + "-ugh/---" });
      }

      commitId_01 = await Operations.Upload(commit);

      Assert.NotNull(commitId_01);
      TestContext.Out.WriteLine($"Written {numObjects + 1} objects. Commit id is {commitId_01}");

      var commitPulled = (Commit)await Operations.Download(commitId_01);

      Assert.AreEqual(commitPulled.GetType(), typeof(Commit));
      Assert.AreEqual(commitPulled.Objects.Count, 30);
    }

    [Test(Description = "Pushing and pulling a random object, with our without detachment"), Order(3)]
    public async Task UploadDownloadNonCommitObject()
    {
      var commit = new Base();
      // Here we are creating a "non-standard" object to act as a base for our multiple objects.
      ((dynamic)commit).LayerA = new List<Base>(); // Layer a and b will be stored "in" the parent object,
      ((dynamic)commit).LayerB = new List<Base>();
      ((dynamic)commit)["@LayerC"] = new List<Base>(); // whereas this "layer" will be stored as references only.
      ((dynamic)commit)["@LayerD"] = new Point[] { new Point(), new Point(12, 3, 4) };
      var rand = new Random();

      for (int i = 0; i < 30; i++)
      {
        ((List<Base>)((dynamic)commit).LayerA).Add(new Point(i, i, i + rand.NextDouble()) { applicationId = i + "foo" });
      }

      for (int i = 0; i < 30; i++)
      {
        ((List<Base>)((dynamic)commit).LayerB).Add(new Point(i, i, i + rand.NextDouble()) { applicationId = i + "bar" });
      }

      for (int i = 0; i < 30; i++)
      {
        ((List<Base>)((dynamic)commit)["@LayerC"]).Add(new Point(i, i, i + rand.NextDouble()) { applicationId = i + "baz" });
      }

      commitId_01 = await Operations.Upload(commit);

      Assert.NotNull(commitId_01);
      TestContext.Out.WriteLine($"Written {numObjects + 1} objects. Commit id is {commitId_01}");

      var commitPulled = (Base)await Operations.Download(commitId_01);

      Assert.AreEqual(commitPulled.GetType(), typeof(Base));

      // Note: even if the layers were originally declared as lists of "Base" objects, on deserialisation we cannot know that,
      // as it's a dynamic property. Dynamic properties, if their content value is ambigous, will default to a common-sense standard. 
      // This specifically manifests in the case of lists and dictionaries: List<AnySpecificType> will become List<object>, and
      // Dictionary<string, MyType> will deserialize to Dictionary<string,object>. 
      var layerA = ((dynamic)commitPulled)["LayerA"] as List<object>;
      Assert.AreEqual(layerA.Count, 30);

      var layerC = ((dynamic)commitPulled)["@LayerC"] as List<object>;
      Assert.AreEqual(layerC.Count, 30);
      Assert.AreEqual(layerC[0].GetType(), typeof(Point));

      var layerD = ((dynamic)commitPulled)["@LayerD"] as List<object>;
      Assert.AreEqual(2, layerD.Count);
    }

    [Test(Description = "Should show progress!"), Order(4)]
    public async Task UploadProgressReports()
    {
      var commit = new Commit();
      var rand = new Random();

      for (int i = 0; i < 30; i++)
      {
        commit.Objects.Add(new Point(i, i, i + rand.NextDouble()) { applicationId = i + "-fab/---" });
      }

      ConcurrentDictionary<string, int> progress = null;
      commitId_02 = await Operations.Upload(commit, onProgressAction: (dict) =>
      {
        progress = dict;
      });

      Assert.NotNull(progress);
      Assert.Contains("Serialization", progress.Keys.ToArray());
    }

    [Test(Description = "Should show progress!"), Order(5)]
    public async Task DownloadProgressReports()
    {
      ConcurrentDictionary<string, int> progress = null;
      var pulledCommit = await Operations.Download(commitId_02, onProgressAction: (dict) =>
      {
        progress = dict;
      });
      Assert.NotNull(progress);
      Assert.Contains("Deserialization", progress.Keys.ToArray());
    }


  }
}