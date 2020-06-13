using Speckle.Models;
using System.Collections.Generic;
using Speckle.Core2;
using System.Diagnostics;
using System;
using NUnit.Framework;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;

namespace Tests
{
  [TestFixture]
  public class PushPull
  {

    [SetUp]
    public void SetUp() { }

    [TearDown]
    public void TearDown() { }

    string commitId;

    [Test]
    public async Task PushPullLocal()
    {
      var myStream = new Speckle.Core2.Stream();

      var objects = new List<Base>();

      var numObjects = 100;
      for (int i = 0; i < numObjects; i++)
      {
        if (i % 2 == 0)
        {
          objects.Add(new Point(i, i, i));
        }
        else
        {
          objects.Add(new Polyline { Points = new List<Point>() { new Point(i * 3, i * 3, i * 3), new Point(i / 2, i / 2, i / 2) } });
        }
      }

      commitId = await myStream.PushLocal(objects);

      var localObjects = await myStream.PullLocal(commitId);

      Assert.AreEqual(100, localObjects.Count);
      Assert.AreEqual(((Point)localObjects[0]).X, 0);
    }

    [Test]
    public void StreamObjects()
    {

      var HttpClient = new HttpClient(new HttpClientHandler()
      {
        AutomaticDecompression = System.Net.DecompressionMethods.GZip,
      }, true);

      HttpClient.BaseAddress = new Uri(@"http://localhost:3000");


      string[] inputFilePaths = Directory.GetFiles(@"/Users/Bender/.config/Speckle/Streams");
      var memStream = new MemoryStream();
      
      //foreach (var file in inputFilePaths)
      //{
      //  using (var inp = File.OpenRead(file))
      //  {
      //    Debug.WriteLine(inp);
      //    inp.CopyTo(memStream);
      //  }
      //}
      //var SourceStream = File.Open(@" / Users/Bender/.config/Speckle/Streams/36dc3ba3-5bf5-4273-ba76-6bd6314b6b36", FileMode.Open);

      //var test = HttpClient.PostAsync("/streaming", new StreamContent(memStream)).Result;

      return;
    }


  }
}
