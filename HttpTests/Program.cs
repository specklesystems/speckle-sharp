using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Speckle.Core;
using Speckle.Models;
using Speckle.Serialisation;
using Speckle.Transports;
using Tests;

namespace HttpTests
{
  class Program
  {
    static async Task Main(string[] args)
    {
      Console.Clear();
      //await BufferedWriteTest();

      //await Whapp();
      //TestCase();
      await SerializedBuffering(5_000);

      Console.WriteLine("Press any key to exit");
      Console.ReadLine();

      return;
    }

    public static void TestCase()
    {
      string commitId;
      int numObjects = 3_000;
      var commit = new Commit();
      var rand = new Random();

      for (int i = 0; i < numObjects; i++)
      {
        commit.Objects.Add(new Point(i, i, i) { applicationId = i + "-id" });
      }

      commitId = Operations.Push(commit).Result;

      var commitPulled = (Commit)Operations.Pull(commitId).Result;
    }

    public static async Task Whapp()
    {
      var objects = new List<Base>();
      var dict = new Dictionary<string, Base>();

      for (int i = 0; i < 10; i++)
      {
        if (i % 2 == 0)
        {
          objects.Add(new Point(i / 5, i / 2, i + 12.3233));
          dict.Add($"key-{i}", new Point(i, i, i));
        }
      }

      var (s, st) = Operations.GetSerializerInstance();
      s.Transport = new SqlLiteObjectTransport();
      var test = JsonConvert.SerializeObject(dict, st);
      var cp = test;
    }

    public static async Task SerializedBuffering(int numObjects = 3000)
    {
      var sw = new Stopwatch();
      sw.Start();

      var objects = new List<Base>();
      for (int i = 0; i < numObjects; i++)
      {
        if (i % 2 == 0)
        {
          objects.Add(new Point(i / 5, i / 2, i + 12.3233));
          //((dynamic)objects[i])["@bobba"] = new Point(2 + i + i, 42, i);
        }
        else
        {
          objects.Add(new Polyline { Points = new List<Point>() { new Point(i * 3.23, i / 3 * 7, i * 3), new Point(i / 2, i / 2, i / 2) } });
          for (int j = 0; j < 54; j++)
          {
            ((Polyline)objects[i]).Points.Add(new Point(j + i, 12.2 + j, 42.32 + j));
          }
        }
      }

      var commit = new Commit();
      commit.Objects = objects;

      var step = sw.ElapsedMilliseconds;
      Console.WriteLine($"Finished generating {numObjects} objs in ${sw.ElapsedMilliseconds / 1000f} seconds.");

      //Dictionary<string, int> progress = new Dictionary<string, int>();
      Console.Clear();
      var progressAction = new Action<ConcurrentDictionary<string, int>>((progress) =>
      {
        Console.CursorLeft = 0;
        Console.CursorTop = 0;

        foreach (var kvp in progress)
          Console.WriteLine($">>> {kvp.Key} progress: {kvp.Value} / {numObjects + 1}");
      });

      var res = await Operations.Push(commit, new SqlLiteObjectTransport(), new Remote[] { new Remote() { ApiToken = "lol", Email = "lol", ServerUrl = "http://localhost:3000", StreamId = "lol" } }, progressAction);

      var cp = res;
      Console.Clear();
      Console.WriteLine($"Finished sending {numObjects} objs or more in ${(sw.ElapsedMilliseconds - step) / 1000f} seconds.");
      Console.WriteLine($"Parent object id: {res}");

      var res2 = await Operations.Pull(res, remote: new Remote() { ApiToken = "lol", Email = "lol", ServerUrl = "http://localhost:3000", StreamId = "lol" }, onProgressAction: dict =>
      {
        Console.CursorLeft = 0;
        Console.CursorTop = 0;

        foreach (var kvp in dict)
          Console.WriteLine($"<<<< {kvp.Key} progress: {kvp.Value} / {numObjects + 1}");
      });

      var cp2 = res2;
      Console.WriteLine($"Pulled (locally) obj");
    }

    public static async Task BufferedWriteTest()
    {
      int numObjects = 100_000;
      var transport = new SqlLiteObjectTransport();
      var rand = new Random();
      var stopWatch = new Stopwatch();

      Console.WriteLine($"-------------------------------------------------\n");
      Console.WriteLine($"Starting to save {numObjects} of objects");

      stopWatch.Start();

      for (int i = 0; i < numObjects; i++)
      {
        //var hash = Speckle.Models.Utilities.hashString($"hash-{i}-{rand.NextDouble()}");
        transport.SaveObject($"hash-{i}-{rand.NextDouble()}", $"content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}");
      }

      // waits for the buffer to be empty. 
      await transport.WriteComplete();

      var stopWatchStep = stopWatch.ElapsedMilliseconds;
      var objsPerSecond = (double)numObjects / (stopWatchStep / 1000);
      Console.WriteLine($"-------------------------------------------------");
      Console.WriteLine($"BufferedWriteTest: Wrote {numObjects} in {stopWatchStep} ms -> {objsPerSecond} objects per second");
      Console.WriteLine($"-------------------------------------------------\n");
    }
  }
}
