using BenchmarkDotNet.Attributes;
using Speckle.Core.Api;
using Speckle.Core.Models;

namespace TestsPerformance.Api.Operations;

[MemoryDiagnoser]
public class Receive : RegressionTestConfig
{
  [Params(10)]
  public int N;

  [Benchmark]
  public async Task<Base> HelpersReceive()
  {
    return await Helpers.Receive($"https://latest.speckle.dev/streams/efd2c6a31d/branches/{N}").ConfigureAwait(false);
  }
}
