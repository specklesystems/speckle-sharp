using Speckle.Core.Api;
using Speckle.Core.Credentials;
using GraphQL;

namespace TestsIntegration
{
  public class GraphQLClientTests
  {
    private Account account;

    [OneTimeSetUp]
    public async Task Setup()
    {
      account = await Fixtures.SeedUser();
    }

    [Test]
    public async Task TestExecuteGraphQLRequest()
    {
      account.serverInfo.url = "http://localhost:8000";
      var client = new Client(account);

      var response = await client.ExecuteGraphQLRequest<ServerInfoResponse>(
        new GraphQLRequest
        {
          Query =
            @"query {
                adminStreams {
                  totalCount
                } 
            }"
        }
      );
      Assert.NotNull(response.serverInfo.version);
    }
  }
}
