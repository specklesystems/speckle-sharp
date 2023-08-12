using BenchmarkDotNet.Running;
using Speckle.Core.Api;
using TestsPerformance.Api.Operations;

namespace TestsPerformance;

public static class Program
{
  public static async Task Main(string[] args)
  {
    BenchmarkSwitcher.FromAssemblies(new[] { typeof(Program).Assembly }).Run(args);
    // var s = await Helpers.Receive($"https://latest.speckle.dev/streams/efd2c6a31d/branches/{2}").ConfigureAwait(false);
    // Console.WriteLine(s.ToString());
  }
}
