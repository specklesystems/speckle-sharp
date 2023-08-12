using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;

namespace TestsPerformance;

[Config(typeof(Config))]
public abstract class RegressionTestConfig
{
  private class Config : ManualConfig
  {
    const int LaunchCount = 1;
    const int WarmupCount = 1;
    const int IterationCount = 5;

    public Config()
    {
      const RunStrategy strategy = RunStrategy.Monitoring;

      AddJob(
        new Job("Head")
          .WithStrategy(strategy)
          .WithLaunchCount(LaunchCount)
          .WithWarmupCount(WarmupCount)
          .WithIterationCount(IterationCount)
          .AsBaseline()
          .Freeze()
      );

      AddJob(
        new Job("2.15.3")
          .WithStrategy(strategy)
          .WithLaunchCount(LaunchCount)
          .WithWarmupCount(WarmupCount)
          .WithIterationCount(IterationCount)
          .WithNuGet("Speckle.Objects", "2.15.3")
          .Freeze()
      );
    }
  }
}
