using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Speckle.Core2;
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
      int numObjects = 100000;
      Console.WriteLine($"-------------------------------------------------\n\n");
      Console.WriteLine($"Starting to save {numObjects} of objects");

      var stopWatch = new Stopwatch();
      stopWatch.Start();

      var test = new SqlLiteObjectTransport();

      await BufferedWriteTest();

      await BulkWriteMany();
      return;

      var objects = new List<Base>();      
      for (int i = 0; i < 100000; i++)
      {
        if (i % 2 == 0)
        {
          objects.Add(new Point(i, i, i));
          //((dynamic)objects[i])["@detachment"] = new Point(i, 20, i);
        }
        else
        {
          objects.Add(new Polyline { Points = new List<Point>() { new Point(i * 3, i * 3, i * 3), new Point(i / 2, i / 2, i / 2) } });
        }
      }

      var commit = new Commit();
      commit.Objects = objects;

      var step = stopWatch.ElapsedMilliseconds;
      //Console.WriteLine("Comm hash: " + commit.hash);
      

      var Serializer = new Serializer();
      var res = Serializer.SerializeAndSave(commit, test);
      var cp = res;
      Console.WriteLine($"saving took ${(stopWatch.ElapsedMilliseconds - step)} miliseconds to compute");
      Console.ReadLine();
    }

    public static async Task SerializedBuffering()
    {
      // TODO
    }

    public static async Task BufferedWriteTest()
    {
      int numObjects = 100000;
      var transport = new SqlLiteObjectTransport();
      var rand = new Random();
      var stopWatch = new Stopwatch();

      Console.WriteLine($"-------------------------------------------------\n");
      Console.WriteLine($"Starting to save {numObjects} of objects");

      stopWatch.Start();

      for (int i = 0; i < numObjects; i++)
      {
        var hash = Speckle.Models.Utilities.hashString($"hash-{i}-{rand.NextDouble()}");
        transport.SaveObject(hash, $"content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}");
      }
      await transport.WriteComplete();

      var stopWatchStep = stopWatch.ElapsedMilliseconds;
      var objsPerSecond = (double) numObjects / (stopWatchStep / 1000);
      Console.WriteLine($"-------------------------------------------------");
      Console.WriteLine($"BufferedWriteTest: Wrote {numObjects} in {stopWatchStep} ms -> {objsPerSecond} objects per second");
      Console.WriteLine($"-------------------------------------------------\n");
    }

    public static async Task BulkWriteMany()
    {
      int numObjects = 100000;
      var transport = new SqlLiteObjectTransport();
      var rand = new Random();
      var stopWatch = new Stopwatch();

      var objs = new List<(string, string)>();
      for (int i = 0; i < numObjects; i++)
      {
        var hash = Speckle.Models.Utilities.hashString($"hash-{i}-{rand.NextDouble()}");
        objs.Add((hash, $"content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}"));
      }

      stopWatch.Start();
      await transport.SaveObjects(objs);

      var stopWatchStep = stopWatch.ElapsedMilliseconds;
      var objsPerSecond = (double)numObjects / (stopWatchStep / 1000);
      Console.WriteLine($"-------------------------------------------------");
      Console.WriteLine($"BulkWriteMany: Wrote {numObjects} in {stopWatchStep} ms -> {objsPerSecond} objects per second");
      Console.WriteLine($"-------------------------------------------------\n");
    }
  }
}
