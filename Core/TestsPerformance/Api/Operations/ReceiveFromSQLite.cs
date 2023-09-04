using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using Speckle.Core.Models;

namespace TestsPerformance.Api.Operations;

[MemoryDiagnoser]
[RegressionTestConfig(1, 1, 8, nugetVersions: "2.15.2")]
public class ReceiveFromSQLite
{
  [Params(0, 4, 9, 19)]
  public int DataComplexity { get; set; }

  private TestDataHelper _dataSource;

  [GlobalSetup]
  public async Task Setup()
  {
    _dataSource = new TestDataHelper();
    await _dataSource.SeedTransport(DataComplexity).ConfigureAwait(false);
  }

  [GlobalCleanup]
  public virtual void Cleanup()
  {
    _dataSource.Dispose();
  }

  [Benchmark]
  public async Task<Base?> Receive_FromSQLite()
  {
    Base? b = await Speckle.Core.Api.Operations
      .Receive(
        _dataSource.ObjectId,
        null,
        _dataSource.Transport,
        onErrorAction: (message, ex) => throw new Exception(message, ex)
      )
      .ConfigureAwait(false);

    Trace.Assert(b is not null);
    return b;
  }
}
