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
        new Point(1,3,4), new Point(4,3,2), new DiningTable(), new Polyline() { Points = new List<Point>(){ new Point(1,3,4), new Point(4,3,2)}}
      };

      for (int i = 0; i < 3; i++)
      {
        if (i % 3 == 0)
          myState.Add(new Polyline() { Points = new List<Point>() { new Point(1, i, i), new Point(4, 3, i) } });
        else
          myState.Add(new DiningTable() { TableModel = i + "-table" });
      }

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
        if (progressCalls++ % 100 == 0)
          Console.WriteLine($"{args.scope}: {args.current} / {args.total}");

        //Debug.Write($"{args.scope}: {args.current} / {args.total}");
        //Console.Write($"{args.scope}: {args.current} / {args.total}");
        //Console.CursorLeft = 0;
        //output.WriteLine($"{args.scope}: {args.current} / {args.total}");
      };

      var myState = new List<Base>();

      for (int i = 0; i < 1000; i++)
      {
        if (i % 3 == 0)
          myState.Add(new Polyline() { Points = new List<Point>() { new Point(1, i, i), new Point(4, 3, i) } });
        else
          myState.Add(new DiningTable() { TableModel = i + "-table", LegOne = new TableLeg() { height = i } });
      }

      stopWatch.Start();

      myModel.SetState(myState);

      Console.WriteLine("");
      Console.WriteLine($"Elapsed: {stopWatch.ElapsedMilliseconds}ms");

      myModel.Commit("lol");

      Console.WriteLine($"Elapsed: {stopWatch.ElapsedMilliseconds}ms");

    }

    [Test]
    public void LoadStreamLocal()
    {
      var myModel = new Stream() { Id = streamId };

      myModel.Add(new Base[] { new Point(-1, -1, -1), new Point(-1, -1, -100) });
      myModel.Commit("added two points");

      myModel.Add(new Base[] { new Point(-112, -1, -1), new Point(1, -1, -100) });
      myModel.Commit("added two more points");


      myModel.Add(new Base[] { new Polyline() { Points = new List<Point>() { new Point(1, 3, 4), new Point(4, 3, 2) } } });
      myModel.Commit("added a polyline");

      var latestRevisionId = myModel.CurrentCommit.hash;

      var myOldStream = Stream.Load(streamId);

      Assert.NotNull(myOldStream.CurrentCommit);

      Assert.AreEqual(3, myOldStream.Commits.Count);

      Assert.AreEqual(latestRevisionId, myOldStream.Commits[myOldStream.Commits.Count - 1]);
    }

  }
}
