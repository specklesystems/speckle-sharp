using Speckle.Models;
using System.Collections.Generic;
using Speckle.Core;
using System.Diagnostics;
using System;
using NUnit.Framework;

namespace Tests
{
  [TestFixture]
  public class Streams
  {

    [SetUp]
    public void SetUp() { }

    [TearDown]
    public void TearDown() { }

    // Used for progress reports in tests.
    private void GenericProgressReporter(object sender, ProgressEventArgs args)
    {
      Console.WriteLine($"{args.scope}: {args.current} / {args.total} ({Math.Round(((double)args.current / (double)args.total) * 100, 2)}%)");
    }

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
    public void StreamBranching()
    {
      //
      // Software A
      // Create a stream
      //
      var myModel = new Stream();
      myModel.OnProgress += GenericProgressReporter;

      myModel.Add(new Base[] { new Point(-1, -1, -1), new Point(-1, -1, -100) });
      myModel.Commit();

      myModel.SetState(new Base[] { new DiningTable() });
      myModel.Commit(branchName: "table-branch");

      myModel.SetState(new Base[] { new Polyline() { applicationId = "test" } });
      myModel.Commit(branchName: "polyline-branch");

      //
      // Software B
      // Retrieve the stream
      // 
      var receiver = Stream.Load(myModel.Id, "polyline-branch");
      receiver.OnProgress += GenericProgressReporter;

      Assert.AreEqual(receiver.CurrentCommit.Objects.Count, 1);
      Assert.AreEqual(receiver.CurrentCommit.Objects[0].applicationId, "test");

      receiver.Checkout("master");
      Assert.AreEqual(2, receiver.CurrentCommit.Objects.Count);

      receiver.Checkout("table-branch");
      //Assert.AreEqual(receiver.CurrentCommit.Objects[0].hash, new DiningTable().hash);

      //
      // Software B
      // Add some more objects - modify it!
      // 
      receiver.Add(new Base[] { new DiningTable() { TableModel = "Super Table Model" }, new DiningTable() { TableModel = "Super Table Model TWO" } });
      receiver.Commit();

      receiver.SetState(new Base[] { new Point(42, 42, 42) });
      receiver.Commit("fed up of tables on this branch");

      //
      // Software C
      // Retrieve the stream... again
      //

      var theSameModel = Stream.Load(myModel.Id);
      theSameModel.OnProgress += GenericProgressReporter;

      theSameModel.Checkout("table-branch");
      Assert.AreEqual(1, theSameModel.CurrentCommit.Objects.Count);
      Assert.AreEqual("fed up of tables on this branch", theSameModel.CurrentCommit.Description);

    }

    [Test]
    public void StreamTagging()
    {
      Assert.Fail(); // TODO
    }

    [Test(Description = "Loads a stream from the users's local machine.")]
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
      Assert.Multiple(() =>
      {
        Assert.NotNull(loadedStream.GetDefaultBranch());

        Assert.Greater(loadedStream.Branches.Count, 0);

        Assert.NotNull(loadedStream.CurrentCommit);

        Assert.AreEqual(loadedStream.CurrentCommit.Objects.Count, 5);

        Assert.AreEqual(3, loadedStream.GetDefaultBranch().Commits.Count);

        //Assert.AreEqual(myModel.CurrentCommit.hash, loadedStream.GetDefaultBranch().Head);
      });
    }
  }
}
