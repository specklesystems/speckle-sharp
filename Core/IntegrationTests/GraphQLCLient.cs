using Speckle.Core.Api;
using Speckle.Core.Credentials;
using GraphQL;
using System.Diagnostics;

namespace TestsIntegration
{
  public class GraphQLClientTests
  {
    private Account _account;
    private Client _client;

    [OneTimeSetUp]
    public async Task Setup()
    {
      _account = await Fixtures.SeedUser();
      _client = new Client(_account);
    }

    [Test]
    public async Task ThrowsForbiddenException()
    {
      Assert.ThrowsAsync<SpeckleGraphQLForbiddenException<Dictionary<string, object>>>(
        async () =>
          await _client.ExecuteGraphQLRequest<Dictionary<string, object>>(
            new GraphQLRequest
            {
              Query =
                @"query {
            adminStreams{
              totalCount
              }
            }"
            },
            CancellationToken.None
          )
      );
    }
  }
}
