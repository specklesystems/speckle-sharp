using Speckle.Models;
using System.Collections.Generic;
using Speckle.Core2;
using System.Diagnostics;
using System;
using NUnit.Framework;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json;
using Speckle.Transports;
using System.Text;

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
    Speckle.Core2.Stream myStream;

    [Test]
    public async Task PushPullLocal()
    {
      myStream = new Speckle.Core2.Stream();

      var objects = new List<Base>();

      var numObjects = 100;
      for (int i = 0; i < numObjects; i++)
      {
        if (i % 2 == 0)
        {
          objects.Add(new Point(i, i, i));
          ((dynamic)objects[i])["@detachment"] = new Point(i, 20, i);
        }
        else
        {
          objects.Add(new Polyline { Points = new List<Point>() { new Point(i * 3, i * 3, i * 3), new Point(i / 2, i / 2, i / 2) } });
        }
      }

      Console.WriteLine("Done adding objects");
      commitId = await myStream.PushLocal(objects);
      Console.WriteLine("Done pushing locally");

      //var localObjects = await myStream.PullLocal(commitId);

      //Assert.AreEqual(numObjects, localObjects.Count);
      //Assert.AreEqual(((Point)localObjects[0]).X, 0);
    }

    [Test]
    public async Task StreamObjectsProofOfConcept()
    {

      var objectIds = (JsonConvert.DeserializeObject<ShallowCommit>(myStream.LocalObjectTransport.GetObject("f81b3937d13d0c439922b7dec138b4e7"))).GetAllObjectIds();

      var HttpClient = new HttpClient(new HttpClientHandler()
      {
        AutomaticDecompression = System.Net.DecompressionMethods.GZip,
      }, true);

      HttpClient.BaseAddress = new Uri(@"http://localhost:3000");

      var multiForm = new MultipartFormDataContent();

      foreach (var objId in objectIds)
      {
        multiForm.Add(new StreamContent(((DiskTransport)myStream.LocalObjectTransport).GetObjectStream(objId)), objId, objId);
      }

      var message = new HttpRequestMessage()
      {
        RequestUri = new Uri("/streaming", UriKind.Relative),
        Method = HttpMethod.Post,
        Content = multiForm
      };

      var res = await HttpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead);

      int bufferSize = 15;

      using (var stream = await res.Content.ReadAsStreamAsync())
      {
        var buffer = new byte[bufferSize];
        int read = -1;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
          var text = Encoding.UTF8.GetString(buffer);
          Console.WriteLine(text);
        }
        // var bytesread = stream.Read(bytes, 0, 1000);
        stream.Close();
      }
      using (var stream = await res.Content.ReadAsStreamAsync())
      {
        var buffer = new byte[bufferSize];
        int read = -1;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
          var text = Encoding.UTF8.GetString(buffer);
          Console.WriteLine(text);
        }
        // var bytesread = stream.Read(bytes, 0, 1000);
        stream.Close();
      }
    }


  }
}
