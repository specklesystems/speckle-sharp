using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Speckle;
using Speckle.Core;
using Speckle.Credentials;
using Speckle.Models;
using Speckle.Serialisation;
using Speckle.Transports;
using Tests;

/// <summary>
/// Quick and dirty tests, specifically involving a remote transport. Once we'll have a test server up and running, I assume these will migrate to tests.  
/// </summary>
namespace ConsoleSketches
{
  class Program
  {
    static async Task Main(string[] args)
    {
      Console.Clear();

      //await PushAndPullToRemote(10_000);

      Console.Clear();

      //await LargeSingleObjects(50_000); // pass in 500k for a 500k vertices mesh. gzipped size: ± 5.4mb, decompressed json: 21.7mb. Works :) 

      Console.Clear();

      //await ManyLargeObjects(); // defaults to 10k meshes with 1k vertices and faces

      //await ValidateAccount();

      await Auth();

      Console.WriteLine("Press any key to exit");
      Console.ReadLine();

      return;
    }

    public static async Task Auth()
    {
      // First log in (uncomment the two lines below to simulate a login/register)
      //var myAccount = await AccountManager.Authenticate("http://localhost:3000");
      //Console.WriteLine($"Succesfully added/updated account server: {myAccount.serverInfo.url} user email: {myAccount.userInfo.email}");

      //// Second log in (make sure you use two different email addresses :D )
      //var myAccount2 = await AccountManager.Authenticate("http://localhost:3000");

      //AccountManager.SetDefaultAccount(myAccount2.id);

      var accs = AccountManager.GetAllAccounts().ToList();
      var deff = AccountManager.GetDefaultAccount();

      //Console.WriteLine($"There are {accs.Count} accounts. The default one is {deff.id} {deff.userInfo.email}");

      foreach (var acc in accs)
      {
        var res = await acc.Validate();

        if (res != null)
          Console.WriteLine($"Validated {acc.id}: {acc.serverInfo.url} / {res.email} / {res.name} (token: {acc.token})");
        else
          Console.WriteLine($"Failed to validate acc {acc.id} / {acc.serverInfo.url} / {acc.userInfo.email}");

        try
        {
          await acc.RotateToken();
          Console.WriteLine($"Rotated {acc.id}: {acc.serverInfo.url} / {res.email} / {res.name} (token: {acc.token})");
        } catch(Exception e)
        {
          Console.WriteLine($"Failed to rotate acc {acc.id} / {acc.serverInfo.url} / {acc.userInfo.email}");
        }

      }
    }

    public static async Task ManyLargeObjects(int numVertices = 1000, int numObjects = 10_000)
    {
      var objs = new List<Base>();

      Console.WriteLine($"Generating {numObjects} meshes with {numVertices} each. That's like {numVertices*numObjects} points. Might take a while?");

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

      var myRemote = new Remote() { ApiToken = "lol", ServerUrl = "http://localhost:3000", StreamId = "lol" };

      var res = await Operations.Upload(
        @object: new Commit() { Objects = objs },
        remotes: new Remote[] { myRemote },
        onProgressAction: dict =>
        {
          Console.CursorLeft = 0;
          Console.CursorTop = 2;

          foreach (var kvp in dict)
            Console.WriteLine($"<<<< {kvp.Key} progress: {kvp.Value} / {numObjects + 1}");
        });

      Console.WriteLine($"Big commit id is {res}");
      
      var receivedCommit = await Operations.Download(res, remote: myRemote, onProgressAction: dict =>
      {
        Console.CursorLeft = 0;
        Console.CursorTop = 7;

        foreach (var kvp in dict)
          Console.WriteLine($"<<<< {kvp.Key} progress: {kvp.Value} / {numObjects + 1}");
      });

      Console.Clear();
      Console.WriteLine($"Received big commit {res}");
    }

    public static async Task LargeSingleObjects(int numVertices = 100_000)
    {
      Console.Clear();
      Console.WriteLine($"Big mesh time! ({numVertices} vertices, and some {numVertices * 1.5} faces");
      var myMesh = new Mesh();

      for (int i = 1; i <= numVertices; i++)
      {
        myMesh.Points.Add(new Point(i / 0.3f, 2 * i + 3.2301000111, 0.22229 * i));
        myMesh.Faces.AddRange(new int[] { i, i + i, i + 3, 23 + i, 100 % i });
      }

      var myRemote = new Remote() { ApiToken = "lol", ServerUrl = "http://localhost:3000", StreamId = "lol" };

      var res = await Operations.Upload(
        @object: myMesh,
        remotes: new Remote[] { myRemote });

      Console.WriteLine($"Big mesh id is {res}");

      var cp = res;

      var pullMyMesh = await Operations.Download(res);

      Console.WriteLine("Pulled back big mesh.");
    }

    public static async Task PushAndPullToRemote(int numObjects = 3000)
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
          objects.Add(new Polyline { Points = new List<Point>() { new Point(i * 3.23, i / 3 * 7, i * 3), new Point(i / 2, i / 2, i / 2) } });
          for (int j = 0; j < 54; j++)
          {
            ((Polyline)objects[i]).Points.Add(new Point(j + i, 12.2 + j, 42.32 + j));
          }
        }
      }

      // Store them into a commit object
      //var commit = new Commit();
      //commit.Objects = objects;

      // Or... in any other type of object you would want to.
      var funkyStructure = new Base();
      ((dynamic)funkyStructure)["@LolLayer"] = objects.GetRange(0, 30);
      ((dynamic)funkyStructure)["@WooTLayer"] = objects.GetRange(30, 100);
      ((dynamic)funkyStructure)["@FTW"] = objects.GetRange(100, objects.Count - 1);


      var step = sw.ElapsedMilliseconds;
      Console.WriteLine($"Finished generating {numObjects} objs in ${sw.ElapsedMilliseconds / 1000f} seconds.");

      Console.Clear();

      // This action will get invoked on progress.
      var pushProgressAction = new Action<ConcurrentDictionary<string, int>>((progress) =>
      {
        Console.CursorLeft = 0;
        Console.CursorTop = 0;

        // The provided dictionary has an individual kvp for each provided transport/process.
        foreach (var kvp in progress)
          Console.WriteLine($">>> {kvp.Key} : {kvp.Value} / {numObjects + 1}");
      });


      // Finally, let's push the commit object. The push action returns the object's id (hash!) that you can use downstream.

      // Create some remotes
      var myRemote = new Remote() { ApiToken = "lol", ServerUrl = "http://localhost:3000", StreamId = "lol" };
      var mySecondRemote = new Remote() { ApiToken = "lol", ServerUrl = "http://localhost:3000", StreamId = "lol" };

      var res = await Operations.Upload(
        @object: funkyStructure,
        remotes: new Remote[] { myRemote, mySecondRemote },
        onProgressAction: pushProgressAction);

      Console.Clear();
      Console.CursorLeft = 0;
      Console.CursorTop = 0;

      Console.WriteLine($"Finished sending {numObjects} objs or more in ${(sw.ElapsedMilliseconds - step) / 1000f} seconds.");
      Console.WriteLine($"Parent object id: {res}");

      Console.Clear();

      // Time for pulling an object out. 
      var res2 = await Operations.Download(res, remote: new Remote() { ApiToken = "lol", ServerUrl = "http://localhost:3000", StreamId = "lol" }, onProgressAction: dict =>
      {
        Console.CursorLeft = 0;
        Console.CursorTop = 0;

        foreach (var kvp in dict)
          Console.WriteLine($"<<<< {kvp.Key} progress: {kvp.Value} / {numObjects + 1}");
      });
      Console.Clear();
      Console.WriteLine("Got those objects back");
    }

    // Some stress tests for the sqlite local transport.
    public static async Task SqliteStressTest()
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
