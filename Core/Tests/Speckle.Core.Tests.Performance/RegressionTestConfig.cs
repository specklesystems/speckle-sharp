using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace Speckle.Core.Tests.Performance;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class, AllowMultiple = true)]
[SuppressMessage(
  "Design",
  "CA1019:Define accessors for attribute arguments",
  Justification = "Suggestion does not fit with IConfigSource pattern"
)]
public sealed class RegressionTestConfigAttribute : Attribute, IConfigSource
{
  public IConfig Config { get; private set; }

  public RegressionTestConfigAttribute(
    int launchCount = 1,
    int warmupCount = 0,
    int iterationCount = 10,
    RunStrategy strategy = RunStrategy.Monitoring,
    bool includeHead = true,
    params string[] nugetVersions
  )
  {
    List<Job> jobs = new();

    if (includeHead)
    {
      jobs.Add(
        new Job("Head")
          .WithRuntime(ClrRuntime.Net481)
          .WithStrategy(strategy)
          .WithLaunchCount(launchCount)
          .WithWarmupCount(warmupCount)
          .WithIterationCount(iterationCount)
      );
    }

    bool isBaseline = true;
    foreach (var version in nugetVersions)
    {
      jobs.Add(
        new Job(version)
          .WithRuntime(ClrRuntime.Net481)
          .WithStrategy(strategy)
          .WithLaunchCount(launchCount)
          .WithWarmupCount(warmupCount)
          .WithIterationCount(iterationCount)
          .WithNuGet("Speckle.Objects", version)
          .WithBaseline(isBaseline)
      );

      isBaseline = false;
    }

    Config = ManualConfig.CreateEmpty().AddJob(jobs.ToArray());
  }
}
