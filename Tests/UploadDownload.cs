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
    string objId_01, commitId_02;
    int numObjects = 3001;


    [Test(Description = "Pushing a commit locally"), Order(1)]
    public void LocalUpload()
    {
      var list = new BaseList();
      var rand = new Random();

      for (int i = 0; i < numObjects; i++)
      {
        list.Items.Add(new Point(i, i, i + rand.NextDouble()) { applicationId = i + "-___/---" });
      }

      objId_01 = Operations.Send(list).Result;

      Assert.NotNull(objId_01);
      TestContext.Out.WriteLine($"Written {numObjects + 1} objects. Commit id is {objId_01}");

    }

    [Test(Description = "Pulling a commit locally"), Order(2)]
    public void LocalDownload()
    {
      var commitPulled = (BaseList)Operations.Receive(objId_01).Result;

      Assert.AreEqual(commitPulled.GetType(), typeof(BaseList));
      Assert.AreEqual(commitPulled.Items[0].GetType(), typeof(Point));
      Assert.AreEqual(commitPulled.Items.Count, numObjects);
    }

    [Test(Description = "Pushing and Pulling a commit locally")]
    public void LocalUploadDownload()
    {
      var baseObjects = new List<Base>();
      var rand = new Random();

      for (int i = 0; i < numObjects; i++)
      {
        baseObjects.Add(new Point(i, i, i + rand.NextDouble()) { applicationId = i + "-___/---" });
      }

      var list = new BaseList();
      list.Items.AddRange(baseObjects);
      objId_01 = Operations.Send(list).Result;

      var commitPulled = (BaseList)Operations.Receive(objId_01).Result;

      Assert.AreEqual(commitPulled.GetType(), typeof(BaseList));
      Assert.AreEqual(commitPulled.Items[0].GetType(), typeof(Point));
      Assert.AreEqual(commitPulled.Items.Count, numObjects);
    }

    [Test(Description = "Pushing and pulling a commit locally"), Order(3)]
    public async Task LocalUploadDownloadSmall()
    {
      var list = new BaseList();
      var rand = new Random();

      for (int i = 0; i < 30; i++)
      {
        list.Items.Add(new Point(i, i, i + rand.NextDouble()) { applicationId = i + "-ugh/---" });
      }

      objId_01 = await Operations.Send(list);

      Assert.NotNull(objId_01);
      TestContext.Out.WriteLine($"Written {numObjects + 1} objects. Commit id is {objId_01}");

      var objsPulled = (BaseList)await Operations.Receive(objId_01);

      Assert.AreEqual(objsPulled.GetType(), typeof(BaseList));
      Assert.AreEqual(objsPulled.Items.Count, 30);
    }

    [Test(Description = "Pushing and pulling a random object, with our without detachment"), Order(3)]
    public async Task UploadDownloadNonCommitObject()
    {
      var obj = new Base();
      // Here we are creating a "non-standard" object to act as a base for our multiple objects.
      ((dynamic)obj).LayerA = new List<Base>(); // Layer a and b will be stored "in" the parent object,
      ((dynamic)obj).LayerB = new List<Base>();
      ((dynamic)obj)["@LayerC"] = new List<Base>(); // whereas this "layer" will be stored as references only.
      ((dynamic)obj)["@LayerD"] = new Point[] { new Point(), new Point(12, 3, 4) };
      var rand = new Random();

      for (int i = 0; i < 30; i++)
      {
        ((List<Base>)((dynamic)obj).LayerA).Add(new Point(i, i, i + rand.NextDouble()) { applicationId = i + "foo" });
      }

      for (int i = 0; i < 30; i++)
      {
        ((List<Base>)((dynamic)obj).LayerB).Add(new Point(i, i, i + rand.NextDouble()) { applicationId = i + "bar" });
      }

      for (int i = 0; i < 30; i++)
      {
        ((List<Base>)((dynamic)obj)["@LayerC"]).Add(new Point(i, i, i + rand.NextDouble()) { applicationId = i + "baz" });
      }

      objId_01 = await Operations.Send(obj);

      Assert.NotNull(objId_01);
      TestContext.Out.WriteLine($"Written {numObjects + 1} objects. Commit id is {objId_01}");

      var objPulled = (Base)await Operations.Receive(objId_01);

      Assert.AreEqual(objPulled.GetType(), typeof(Base));

      // Note: even if the layers were originally declared as lists of "Base" objects, on deserialisation we cannot know that,
      // as it's a dynamic property. Dynamic properties, if their content value is ambigous, will default to a common-sense standard. 
      // This specifically manifests in the case of lists and dictionaries: List<AnySpecificType> will become List<object>, and
      // Dictionary<string, MyType> will deserialize to Dictionary<string,object>. 
      var layerA = ((dynamic)objPulled)["LayerA"] as List<object>;
      Assert.AreEqual(layerA.Count, 30);

      var layerC = ((dynamic)objPulled)["@LayerC"] as List<object>;
      Assert.AreEqual(layerC.Count, 30);
      Assert.AreEqual(layerC[0].GetType(), typeof(Point));

      var layerD = ((dynamic)objPulled)["@LayerD"] as List<object>;
      Assert.AreEqual(2, layerD.Count);
    }

    [Test(Description = "Should show progress!"), Order(4)]
    public async Task UploadProgressReports()
    {
      var list = new BaseList();
      var rand = new Random();

      for (int i = 0; i < 30; i++)
      {
        list.Items.Add(new Point(i, i, i + rand.NextDouble()) { applicationId = i + "-fab/---" });
      }

      ConcurrentDictionary<string, int> progress = null;
      commitId_02 = await Operations.Send(list, onProgressAction: (dict) =>
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
      var pulledCommit = await Operations.Receive(commitId_02, onProgressAction: (dict) =>
      {
        progress = dict;
      });
      Assert.NotNull(progress);
      Assert.Contains("Deserialization", progress.Keys.ToArray());
    }


  }
}
