using System;
using System.Diagnostics;
using Speckle.Transports;

namespace HttpTests
{
  class Program
  {
    static void Main(string[] args)
    {
      int numObjects = 100000;
      Console.WriteLine($"-------------------------------------------------\n\n");
      Console.WriteLine($"Starting to save {numObjects} of objects");

      var stopWatch = new Stopwatch();
      stopWatch.Start();

      var test = new SQLiteTransport();

      for (int i = 0; i < numObjects; i++)
      {
        test.SaveObject($"hash-{i}", $"content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}content-longer-maye-it's-ok-{i}");
      }

      var stopWatchStep = stopWatch.ElapsedMilliseconds;

      Console.WriteLine($"Wrote {numObjects} in {stopWatchStep} ms");
      Console.WriteLine("Press something to continue");

      Console.ReadLine();

      var bar = test.GetObject($"hash-{numObjects-1}");

      Console.WriteLine($"read back obhect: {bar}");
      Console.ReadLine();

    }
  }
}
