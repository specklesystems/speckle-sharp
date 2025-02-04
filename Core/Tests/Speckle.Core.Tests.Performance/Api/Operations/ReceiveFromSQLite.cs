using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using Speckle.Core.Models;

namespace Speckle.Core.Tests.Performance.Api.Operations;

[MemoryDiagnoser]
[RegressionTestConfig(1, 1, 8, nugetVersions: "2.15.2")]
public class ReceiveFromSQLite : IDisposable
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

  [Benchmark]
  public async Task<Base?> Receive_FromSQLite()
  {
    Base? b = await Speckle
      .Core.Api.Operations.Receive(_dataSource.ObjectId, null, _dataSource.Transport)
      .ConfigureAwait(false);

    Trace.Assert(b is not null);
    return b;
  }

  [GlobalCleanup]
  public virtual void Cleanup()
  {
    Dispose();
  }

  public void Dispose()
  {
    _dataSource.Dispose();
  }
}
