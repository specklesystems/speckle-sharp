using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Speckle.Core.Helpers;
using Serilog;
using Tests;
using Serilog.Context;

/// <summary>
/// Quick and dirty tests/examples of Speckle usage.
/// </summary>
namespace ExampleApp
{
  ////////////////////////////////////////////////////////////////////////////
  /// NOTE:                                                                ///
  /// These tests don't run without a server running locally.              ///
  /// Check out https://github.com/specklesystems/server for               ///
  /// more info on the server.                                             ///
  /// Also, please make sure you have a default speckle account.           ///
  ////////////////////////////////////////////////////////////////////////////

  class Program
  {
    static async Task Main(string[] args)
    {
      // Step 1. make sure to initialize the logger with the proper parameters
      // note, this initialization is part of the current default setup, with sane
      SpeckleLog.Initialize(
        hostApplicationName: "Enterprise ",
        hostApplicationVersion: "NCC-1701-D",
        logConfiguration: new(Serilog.Events.LogEventLevel.Debug, logToConsole: true)
      );

      // by default, this:
      // logs to file in the Speckle folder / Logs / hostApplicationName
      // reports errors to sentry
      // sends all log events above the configured level to seq
      // logs to the stdout console (useful when debugging)
      // note, not all props are available in the file and console log, context is not rendered there

      SpeckleLog.Logger.Information(
        "Captain's first log, stardate {stardate:.0}. This is the beginning of our journey",
        40759.5123
      );

      var log = SpeckleLog.Logger.ForContext("currentSpeed", "warp 5").ForContext("captain", "Jean-Luc Picard");

      SpeckleLog.Logger.Information(
        "We're traveling to {destination} our current speed is {currentSpeed}",
        "Fairpoint station"
      );

      GlobalLogContext.PushProperty("captain", "Picard");

      using (LogContext.PushProperty("actingEnsign", "Wesley Crusher"))
      {
        var bearing = new { bearing = 25, mark = 134 };
        SpeckleLog.Logger.Information("Picking up an anomaly, bearing {@bearing}", bearing);
      }
      try
      {
        SpeckleLog.Logger.Warning("Yellow alert, shields up");
        throw new Exception("Shields are not responding.");
      }
      catch (Exception ex)
      {
        SpeckleLog.Logger.Error(ex, "Cannot raise shields, {reason}", ex.Message);
        SpeckleLog.Logger.Fatal(ex, "Cannot raise shields, {reason}", ex.Message);
      }

      // await Subscriptions.SubscriptionConnection();

      // Console.Clear();

      // //await SendAndReceive(1_000);

      // Console.Clear();

      // //await SendReceiveLargeSingleObjects(50_000); // pass in 500k for a 500k vertices mesh. gzipped size: ± 5.4mb, decompressed json: 21.7mb. Works :)

      // Console.Clear();

      // //await SendReceiveManyLargeObjects(); // defaults to 10k meshes with 1k vertices and faces


      // var foo = new Base() { };

      // var chld1 = new ObjectReference();
      // chld1.referencedId = "f048873d78d8833e1a2c0d7c2391a9bb";

      // foo["Members"] = new[] { chld1 };

      // var ser = Operations.Serialize(foo);

      Console.WriteLine("Press any key to exit");
      Console.ReadLine();

      return;
    }

    /// <summary>
    /// Sends many large objects. It's more of a stress test.
    /// </summary>
    /// <param name="numVertices"></param>
    /// <param name="numObjects"></param>
    /// <returns></returns>
    public static async Task SendReceiveManyLargeObjects(
      int numVertices = 1000,
      int numObjects = 10_000
    )
    {
      var objs = new List<Base>();

      Console.WriteLine(
        $"Generating {numObjects} meshes with {numVertices} each. That's like {numVertices * numObjects} points. Might take a while?"
      );

      for (int i = 1; i <= numObjects; i++)
      {
        Console.CursorTop = 1;
        Console.WriteLine($"Generating mesh {i}");
        var myMesh = new Mesh();
        for (int j = 1; j <= numVertices; j++)
        {
          myMesh.Points.Add(new Point(j / 0.3f, 2 * j + 3.2301000111, 0.22229 * j));
          myMesh.Faces.AddRange(new int[] { j, j + i, j + 3, j + 23 + i, 100 % i + j });
        }
        objs.Add(myMesh);
      }

      Console.Clear();
      Console.WriteLine("Done generating objects.");

      var myClient = new Client(AccountManager.GetDefaultAccount());
      var streamId = await myClient.StreamCreate(
        new StreamCreateInput { name = "test", description = "this is a test" }
      );
      var myServer = new ServerTransport(AccountManager.GetDefaultAccount(), streamId);

      var myObject = new Base();
      myObject["items"] = objs;

      var res = await Operations.Send(
        myObject,
        new List<ITransport>() { myServer },
        onProgressAction: dict =>
        {
          Console.CursorLeft = 0;
          Console.CursorTop = 2;

          foreach (var kvp in dict)
            Console.WriteLine($"<<<< {kvp.Key} progress: {kvp.Value} / {numObjects + 1}");
        }
      );

      Console.WriteLine($"Big commit id is {res}");

      var receivedCommit = await Operations.Receive(
        res,
        remoteTransport: myServer,
        onProgressAction: dict =>
        {
          Console.CursorLeft = 0;
          Console.CursorTop = 7;

          foreach (var kvp in dict)
            Console.WriteLine($"<<<< {kvp.Key} progress: {kvp.Value} / {numObjects + 1}");
        }
      );

      Console.Clear();
      Console.WriteLine($"Received big commit {res}");
    }

    /// <summary>
    /// Speckle 1.0 had some inherited limitations on object size from MongoDB. Since we've moved to postgres, let's flex.
    /// </summary>
    /// <param name="numVertices"></param>
    /// <returns></returns>
    public static async Task SendReceiveLargeSingleObjects(int numVertices = 100_000)
    {
      Console.Clear();
      Console.WriteLine(
        $"Big mesh time! ({numVertices} vertices, and some {numVertices * 1.5} faces"
      );
      var myMesh = new Mesh();

      for (int i = 1; i <= numVertices; i++)
      {
        myMesh.Points.Add(new Point(i / 0.3f, 2 * i + 3.2301000111, 0.22229 * i));
        myMesh.Faces.AddRange(new int[] { i, i + i, i + 3, 23 + i, 100 % i });
      }

      var myClient = new Client(AccountManager.GetDefaultAccount());
      var streamId = await myClient.StreamCreate(
        new StreamCreateInput { name = "test", description = "this is a test" }
      );
      var server = new ServerTransport(AccountManager.GetDefaultAccount(), streamId);

      var res = await Operations.Send(myMesh, transports: new List<ITransport>() { server });
      ;

      Console.WriteLine($"Big mesh id is {res}");

      var cp = res;

      var pullMyMesh = await Operations.Receive(res);

      Console.WriteLine("Pulled back big mesh.");
    }

    /// <summary>
    /// A more generic version of the above.
    /// </summary>
    /// <param name="numObjects"></param>
    /// <returns></returns>
    public static async Task SendAndReceive(int numObjects = 3000)
    {
      var sw = new Stopwatch();
      sw.Start();

      // Create a bunch of objects!
      var objects = new List<Base>();
      for (int i = 0; i < numObjects; i++)
      {
        if (i % 2 == 0)
        {
          objects.Add(new Point(i / 5, i / 2, i + 12.3233));
          ((dynamic)objects[i])["@bobba"] = new Point(2 + i + i, 42, i); // This will throw our progress reporting off, as our total object count will not reflect detached objects. C'est la vie.
        }
        else
        {
          objects.Add(
            new Polyline
            {
              Points = new List<Point>()
              {
                new Point(i * 3.23, i / 3 * 7, i * 3),
                new Point(i / 2, i / 2, i / 2)
              }
            }
          );
          for (int j = 0; j < 54; j++)
          {
            ((Polyline)objects[i]).Points.Add(new Point(j + i, 12.2 + j, 42.32 + j));
          }
        }
      }

      var myRevision = new Base();
      ((dynamic)myRevision)["@LolLayer"] = objects.GetRange(0, 30);
      ((dynamic)myRevision)["@WooTLayer"] = objects.GetRange(30, 100);
      ((dynamic)myRevision)["@FTW"] = objects.GetRange(130, objects.Count - 130 - 1);

      var step = sw.ElapsedMilliseconds;
      Console.WriteLine(
        $"Finished generating {numObjects} objs in ${sw.ElapsedMilliseconds / 1000f} seconds."
      );

      Console.Clear();

      // This action will get invoked on progress.
      var pushProgressAction = new Action<ConcurrentDictionary<string, int>>(
        (progress) =>
        {
          Console.CursorLeft = 0;
          Console.CursorTop = 0;

          // The provided dictionary has an individual kvp for each provided transport/process.
          foreach (var kvp in progress)
            Console.WriteLine($">>> {kvp.Key} : {kvp.Value} / {numObjects + 1}");
        }
      );

      // Let's set up some fake server transports.
      var myClient = new Client(AccountManager.GetDefaultAccount());
      var streamId = await myClient.StreamCreate(
        new StreamCreateInput { name = "test", description = "this is a test" }
      );
      var firstServer = new ServerTransport(AccountManager.GetDefaultAccount(), streamId);

      var mySecondClient = new Client(AccountManager.GetDefaultAccount());
      var secondStreamId = await myClient.StreamCreate(
        new StreamCreateInput { name = "test2", description = "this is a second test" }
      );
      var secondServer = new ServerTransport(AccountManager.GetDefaultAccount(), secondStreamId);

      var res = await Operations.Send(
        @object: myRevision,
        transports: new List<ITransport>() { firstServer, secondServer },
        onProgressAction: pushProgressAction
      );

      Console.Clear();
      Console.CursorLeft = 0;
      Console.CursorTop = 0;

      Console.WriteLine(
        $"Finished sending {numObjects} objs or more in ${(sw.ElapsedMilliseconds - step) / 1000f} seconds."
      );
      Console.WriteLine($"Parent object id: {res}");

      Console.Clear();

      // Time for getting our revision object back.
      var res2 = await Operations.Receive(
        res,
        remoteTransport: firstServer,
        onProgressAction: dict =>
        {
          Console.CursorLeft = 0;
          Console.CursorTop = 0;

          foreach (var kvp in dict)
            Console.WriteLine($"<<<< {kvp.Key} progress: {kvp.Value} / {numObjects + 1}");
        }
      );

      Console.Clear();
      Console.WriteLine("Got those objects back");
    }

    /// <summary>
    /// Some stress tests for the sqlite transport. Perhaps useful later.
    /// </summary>
    /// <returns></returns>
    public static async Task SqliteStressTest()
    {
      int numObjects = 100_000;
      var transport = new SQLiteTransport();
      var rand = new Random();
      var stopWatch = new Stopwatch();

      Console.WriteLine($"-------------------------------------------------\n");
      Console.WriteLine($"Starting to save {numObjects} of objects");

      stopWatch.Start();

      for (int i = 0; i < numObjects; i++)
      {
        //var hash = Speckle.Core.Models.Utilities.hashString($"hash-{i}-{rand.NextDouble()}");
        transport.SaveObject(
          $"hash-{i}-{rand.NextDouble()}",
          $"content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}"
        );
      }

      // waits for the buffer to be empty.
      await transport.WriteComplete();

      var stopWatchStep = stopWatch.ElapsedMilliseconds;
      var objsPerSecond = (double)numObjects / (stopWatchStep / 1000);
      Console.WriteLine($"-------------------------------------------------");
      Console.WriteLine(
        $"BufferedWriteTest: Wrote {numObjects} in {stopWatchStep} ms -> {objsPerSecond} objects per second"
      );
      Console.WriteLine($"-------------------------------------------------\n");
    }
  }
}
