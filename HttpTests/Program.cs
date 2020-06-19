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
      //await BufferedWriteTest();

      //await BulkWriteMany();

      await SerializedBuffering();

      Console.WriteLine("Press any key to exit");
      Console.ReadLine();

      return;
    }

    public static async Task SerializedBuffering()
    {
      int numObjects = 1000;
      var objects = new List<Base>();
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
          for (int j = 0; j < 3000; j++)
          {
            ((Polyline)objects[i]).Points.Add(new Point(j, j, j));
          }
        }
      }

      var p = new Polyline();
      for (int j = 0; j < 50000; j++)
      {
        p.Points.Add(new Point(j, 2, 3));
      }

      var commit = new Commit();
      commit.Objects = objects;
      commit.Objects.Add(p);

      var Serializer = new Serializer();

      var res = await Serializer.Serialize(commit, (string transportName, int totalCount) =>
      {
        //Console.WriteLine($"Transport {transportName} serialized {totalCount} objects out of {numObjects + 1}.");
      });

      var cp = res;
      //var res2 = Serializer.Deserialize(res);
      //var cp2 = res2;
    }

    public static async Task BufferedWriteTest()
    {
      int numObjects = 1000;
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

    public static async Task BulkWriteMany()
    {
      int numObjects = 100000;
      var transport = new SqlLiteObjectTransport();
      var rand = new Random();
      var stopWatch = new Stopwatch();

      var objs = new List<(string, string)>();
      for (int i = 0; i < numObjects; i++)
      {
        //var hash = Speckle.Models.Utilities.hashString();
        objs.Add(($"hash-{i}-{rand.NextDouble()}", $"content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}"));
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
