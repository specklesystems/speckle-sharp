using System.Diagnostics.CodeAnalysis;
using GraphQL;
using Speckle.Core.Api;
using Speckle.Core.Models;

namespace Speckle.Automate.Sdk.Tests.Integration;

public static class TestAutomateUtils
{
  [SuppressMessage("Security", "CA5394:Do not use insecure randomness")]
  public static string RandomString(int length)
  {
    Random rand = new();
    const string POOL = "abcdefghijklmnopqrstuvwxyz0123456789";
    IEnumerable<char> chars = Enumerable.Range(0, length).Select(_ => POOL[rand.Next(0, POOL.Length)]);
    return new string(chars.ToArray());
  }

  public static Base TestObject()
  {
    Base rootObject = new() { ["foo"] = "bar" };
    return rootObject;
  }

  public static async Task RegisterNewAutomation(
    string projectId,
    string modelId,
    Client speckleClient,
    string automationId,
    string automationName,
    string automationRevisionId
  )
  {
    GraphQLRequest query =
      new(
        query: """
        mutation CreateAutomation(
            $projectId: String!
            $modelId: String!
            $automationName: String!
            $automationId: String!
            $automationRevisionId: String!
        ) {
                automationMutations {
                    create(
                        input: {
                            projectId: $projectId
                            modelId: $modelId
                            automationName: $automationName
                            automationId: $automationId
                            automationRevisionId: $automationRevisionId
                        }
                    )
                }
            }
        """,
        variables: new
        {
          projectId,
          modelId,
          automationName,
          automationId,
          automationRevisionId,
        }
      );

    await speckleClient.ExecuteGraphQLRequest<object>(query);
  }
}
