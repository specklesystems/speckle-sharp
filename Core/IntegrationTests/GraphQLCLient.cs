using System.Diagnostics;
using GraphQL;
using Speckle.Core.Api;
using Speckle.Core.Credentials;

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


  }
}
