using Speckle.Core.Api;
using Speckle.Core.Credentials;

namespace TestsIntegration;

[TestOf(typeof(StreamWrapper))]
[TestFixtureSource(nameof(TestData))]
public class StreamWrapperFE2Models
{
  private static IEnumerable<string> TestData()
  {
    yield return "myModel";
    yield return "myOtherModel";
    yield return "theBestModel";
  }

  private readonly string _expectedBranchName;
  private Uri _testUrl;
  private static Client? s_client;

  public StreamWrapperFE2Models(string branchName)
  {
    _expectedBranchName = branchName;
  }

  [OneTimeSetUp]
  public async Task OneTimeSetup()
  {
    s_client ??= new Client(await Fixtures.SeedUser().ConfigureAwait(false));
  }

  [SetUp]
  public async Task SetUp()
  {
    string streamId = await s_client!.StreamCreate(new StreamCreateInput() { name = "myStream" });
    await s_client.BranchCreate(new BranchCreateInput() { name = _expectedBranchName, streamId = streamId });
    Branch branch = await s_client.BranchGet(streamId, _expectedBranchName);

    _testUrl = new($"{s_client.ServerUrl}/projects/{streamId}/models/{branch.id}");
  }

  [Test]
  public async Task StreamWrapper_Parses_FE2_Model()
  {
    var sw = await StreamWrapper.CreateFrom(_testUrl);

    Assert.That(sw.BranchName, Is.EqualTo(_expectedBranchName));
  }
}
