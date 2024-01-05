using BenchmarkDotNet.Running;

namespace Speckle.Core.Tests.Performance;

public static class Program
{
  public static void Main(string[] args)
  {
    BenchmarkSwitcher.FromAssemblies(new[] { typeof(Program).Assembly }).Run(args);
  }
}
