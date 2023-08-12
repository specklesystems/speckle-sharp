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
    const RunStrategy Strategy = RunStrategy.Monitoring;

    public Config()
    {
      AddJob(
        new Job("Head")
          .WithStrategy(Strategy)
          .WithLaunchCount(LaunchCount)
          .WithWarmupCount(WarmupCount)
          .WithIterationCount(IterationCount)
          .Freeze()
      );

      AddJob(
        new Job("2.15.3")
          .WithStrategy(Strategy)
          .WithLaunchCount(LaunchCount)
          .WithWarmupCount(WarmupCount)
          .WithIterationCount(IterationCount)
          .WithNuGet("Speckle.Objects", "2.15.3")
          .AsBaseline()
          .Freeze()
      );

      // AddJob(
      //   new Job("2.14.2")
      //     .WithStrategy(Strategy)
      //     .WithLaunchCount(LaunchCount)
      //     .WithWarmupCount(WarmupCount)
      //     .WithIterationCount(IterationCount)
      //     .WithNuGet("Speckle.Objects", "2.14.2")
      //     .Freeze()
      // );
    }
  }
}
