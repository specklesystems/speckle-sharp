using BenchmarkDotNet.Running;

namespace TestsPerformance;

public static class Program
{
  public static async Task Main(string[] args)
  {
    BenchmarkSwitcher.FromAssemblies(new[] { typeof(Program).Assembly }).Run(args);
  }
}
