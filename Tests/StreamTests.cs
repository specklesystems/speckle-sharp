using Speckle.Models;
using System.Collections.Generic;
using Speckle.Core;
using System.Diagnostics;
using System;
using NUnit.Framework;

namespace Tests
{

  public class Streams
  {

    // One stream id hardcoded, for less garbage creation
    string streamId = "b8efc2d5-d1f8-433d-82b3-9ae67a9d2aae";

    [Test]
    public void CreateAndCommit()
    {
      var myModel = new Stream() { Id = streamId };

      myModel.Name = "Testing Model";

      var myState = new List<Base>()
      {
        new Point(1,3,4),
        new Point(4,3,2),
        new DiningTable(),
        new Polyline() { Points = new List<Point>(){ new Point(1,3,4), new Point(4,3,2)}}
      };

      myModel.SetState(myState);
      myModel.Commit("first commit");

      myModel.Add(new Base[] { new Point(-1, -1, -1), new Point(-1, -1, -100) });
      myModel.Commit("added two more points");
    }

    int progressCalls = 0;
    string secondStreamId = "b8efc2d5-d1f8-433d-82b3-9ae67a9d2aae";

    [Test]
    public void StreamProgressEvents()
    {
      var stopWatch = new Stopwatch();

      var myModel = new Stream() { Id = secondStreamId };
      myModel.Name = "Testing Model";

      myModel.OnProgress += (sender, args) =>
      {
        if (args.total >= 1000)
        {
          if ((progressCalls++ % 100 == 0 || args.current >= args.total)) // emit staggered if more than 1k objs
          {
            Console.WriteLine($"{args.scope}: {args.current} / {args.total} ({Math.Round(((double)args.current / (double)args.total) * 100, 2)}%)");
          }
        }
        else
        {
          Console.WriteLine($"{args.scope}: {args.current} / {args.total} ({Math.Round(((double)args.current / (double)args.total) * 100, 2)}%)");
        }
      };

      var myState = new List<Base>();
      int numObjects = 99;

      for (int i = 0; i < numObjects; i++)
      {
        if (i % 3 == 0)
          myState.Add(new Polyline() { Points = new List<Point>() { new Point(1, i, i), new Point(4, 3, i) } });
        else
          //myState.Add(new DiningTable() { TableModel = i + "-table", LegOne = new TableLeg() { height = i } });
          myState.Add(new Point(10, 12, i));
      }

      stopWatch.Start();

      myModel.SetState(myState);

      myModel.Commit("lol");

      Console.WriteLine($"Elapsed: {stopWatch.ElapsedMilliseconds}ms");

    }

    [Test]
    public void LoadStreamLocal()
    {
      // Create a stream
      var myModel = new Stream() { Id = streamId };

      myModel.OnProgress += (sender, args) =>
      {
        Console.WriteLine($"{args.scope}: {args.current} / {args.total} ({Math.Round(((double)args.current / (double)args.total) * 100, 2)}%)");
      };

      // Create a three of commits:

      // 1) add 2 objs
      myModel.Add(new Base[] { new Point(-1, -1, -1), new Point(-1, -1, -100) });
      myModel.Commit("added two points");

      // 2) add 2 objs
      myModel.Add(new Base[] { new Point(-112, -1, -1), new Point(1, -1, -100) });
      myModel.Commit("added two more points");

      // 3) add 1 obj, total = 5 objs
      myModel.Add(new Base[] { new Polyline() { Points = new List<Point>() { new Point(1, 3, 4), new Point(4, 3, 2) } } });
      myModel.Commit("added a polyline");

      // Receive a stream
      var loadedStream = Stream.Load(streamId, OnProgress: (sender, args) =>
      {
        Console.WriteLine($"{args.scope}: {args.current} / {args.total} ({Math.Round(((double)args.current / (double)args.total) * 100, 2)}%)");
      });

      // Assertion checks
      Assert.NotNull(loadedStream.GetCurrentBranch());

      Assert.Greater(loadedStream.Branches.Count, 0);

      Assert.NotNull(loadedStream.CurrentCommit);

      Assert.AreEqual(loadedStream.CurrentCommit.Objects.Count, 5);

      Assert.AreEqual(3, loadedStream.GetCurrentBranch().Commits.Count);

      Assert.AreEqual(myModel.CurrentCommit.hash, loadedStream.GetCurrentBranch().Head);
    }

  }
}
