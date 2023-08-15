using BenchmarkDotNet.Attributes;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

namespace TestsPerformance.Api.Operations;

[MemoryDiagnoser]
[RegressionTestConfig(1, 1, 20, nugetVersions:"2.15.2")]
public class TraverseCommit
{
  [Params(0, 4, 9, 19)]
  public int DataComplexity { get; set; }

  private Base _testData;
  private GraphTraversal _sut;

  [GlobalSetup]
  public async Task Setup()
  {
    using var dataSource = new TestDataHelper();
    await dataSource.SeedTransport(DataComplexity).ConfigureAwait(false);
    _testData = await dataSource.DeserializeBase().ConfigureAwait(false);

    var convertableRule = TraversalRule
      .NewTraversalRule()
      .When(b => b.speckle_type.Contains("Geometry"))
      .When(DefaultTraversal.HasDisplayValue)
      .ContinueTraversing(_ => DefaultTraversal.elementsPropAliases);

    _sut = new GraphTraversal(convertableRule, DefaultTraversal.DefaultRule);
  }

  [Benchmark]
  public List<TraversalContext> Traverse()
  {
    return _sut.Traverse(_testData).ToList();
  }
}
