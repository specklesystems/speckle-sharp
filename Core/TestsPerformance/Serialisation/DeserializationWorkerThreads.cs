using BenchmarkDotNet.Attributes;
using Speckle.Core.Models;
using Speckle.Core.Serialisation;
using Speckle.Core.Transports;

namespace TestsPerformance.Serialisation;

[MemoryDiagnoser]
[RegressionTestConfig(1, 1, 6)]
public class DeserializationWorkerThreads
{
  public static IEnumerable<int> NumThreadsToTest => Enumerable.Range(0, Environment.ProcessorCount + 1);

  [Params(0, 9)]
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
  [ArgumentsSource(nameof(NumThreadsToTest))]
  public Base RunTest(int numThreads)
  {
    BaseObjectDeserializerV2 sut = new() { WorkerThreadCount = numThreads, ReadTransport = _dataSource.Transport };
    return sut.Deserialize(_dataSource.Transport.GetObject(_dataSource.ObjectId)!);
  }
}
