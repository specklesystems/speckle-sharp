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
      int numObjects = 3;
      Console.WriteLine($"-------------------------------------------------\n\n");
      Console.WriteLine($"Starting to save {numObjects} of objects");

      var stopWatch = new Stopwatch();
      stopWatch.Start();

      var test = new SqlLiteObjectTransport();

      //for (int i = 0; i < numObjects; i++)
      //{
      //  test.SaveObject($"hash-{i}", $"content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}");
      //}

      //var stopWatchStep = stopWatch.ElapsedMilliseconds;

      //Console.WriteLine($"Wrote {numObjects} in {stopWatchStep} ms");
      //Console.WriteLine("Press something to continue");

      //Console.ReadLine();

      //var bar = test.GetObject($"hash-{numObjects-1}");

      //Console.WriteLine($"read back obhect: {bar}");
      //Console.ReadLine();

      //var objs = new List<(string, string)>();
      //for(int i = 0; i < numObjects; i++)
      //{
      //  objs.Add(($"hash-{i}", $"content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}"));
      //}

      //await test.SaveObjects(objs);
      //Console.WriteLine($"Wrote {numObjects} in {stopWatch.ElapsedMilliseconds/1000}s; thats like {numObjects/stopWatch.ElapsedMilliseconds*1000} objs a second");

      var objects = new List<Base>();

      
      for (int i = 0; i < 100000; i++)
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
  }
}
