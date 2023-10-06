using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using GraphQL;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace Speckle.Automate.Sdk.Tests.Integration;

public class AutomationContextTests
{
  
  
  [SuppressMessage("Security", "CA5394:Do not use insecure randomness")]
  private static string RandomString(int length)
  {
    Random rand = new();
    const string pool = "abcdefghijklmnopqrstuvwxyz0123456789";
    var chars = Enumerable.Range(0, length).Select(_ => pool[rand.Next(0, pool.Length)]);
    return new string(chars.ToArray());
  }

  private static async Task RegisterNewAutomation(
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

    await speckleClient.ExecuteGraphQLRequest<bool>(query);
  }

  [Pure]
  private static string GetSpeckleToken(IReadOnlyDictionary<string, string> userDict)
  {
    return userDict["token"];
  }

  [Pure]
  private static string GetSpeckleServerUrl(string host)
  {
    return $"http://{host}";
  }

  private static async Task<Client> GetTestClient(string speckleServerUrl, string speckleToken)
  {
    Account acc =
      new()
      {
        token = speckleToken,
        serverInfo = new ServerInfo() { url = speckleServerUrl },
      };
    await acc.Validate();
    Client testClient = new(acc);
    return testClient;
  }

  private static Base TestObject()
  {
    Base rootObject = new();
    rootObject["foo"] = "bar";
    return rootObject;
  }

  private static async Task<AutomationRunData> AutomationRunData(
    Base testObject,
    Client testClient,
    string speckleServerUrl
  )
  {
    string projectId = await testClient.StreamCreate(new() { name = "Automate function e2e test" });
    const string branchName = "main";

    Branch model = await testClient.BranchGet(projectId, branchName, 1);
    string modelId = model.id;

    string rootObjId = await Operations.Send(
      testObject,
      new List<ITransport> { new ServerTransport(testClient.Account, projectId) }
    );

    string versionId = await testClient.CommitCreate(new() { streamId = projectId, objectId = rootObjId, });

    var automationName = RandomString(10);
    var automationId = RandomString(10);
    var automationRevisionId = RandomString(10);

    await RegisterNewAutomation(projectId, modelId, testClient, automationId, automationName, automationRevisionId);

    var automationRunId = RandomString(10);
    var functionId = RandomString(10);
    var functionRelease = RandomString(10);

    return new AutomationRunData()
    {
      ProjectId = projectId,
      ModelId = modelId,
      BranchName = branchName,
      VersionId = versionId,
      SpeckleServerUrl = speckleServerUrl,
      AutomationId = automationId,
      AutomationRevisionId = automationRevisionId,
      AutomationRunId = automationRunId,
      FunctionId = functionId,
      FunctionRelease = functionRelease,
    };
  }

  private static async Task<dynamic> GetAutomationStatus(string projectId, string modelId, Client speckleClient)
  {
    GraphQLRequest query =
      new(
        """
        query AutomationRuns(
            $projectId: String!
            $modelId: String!
        )
        {
        project(id: $projectId) {
        model(id: $modelId) {
        automationStatus {
        id
        status
        statusMessage
        automationRuns {
          id
          automationId
          versionId
          createdAt
          updatedAt
          status
          functionRuns {
            id
            functionId
            elapsed
            status
            contextView
            statusMessage
            results
            resultVersions {
              id
            }
          }
        }
        }
        }
        }
        }
        """,
        variables: new { projectId, modelId, }
      );
    var response = await speckleClient.ExecuteGraphQLRequest<Dictionary<string, dynamic>>(query);
    return response["project"]["model"]["automationStatus"];
  }

  private async Task TestFunctionRun(AutomationRunData automationRunData, string speckleToken)
  {
    var automationContext = await AutomationRunner.RunFunction(
      AutomateFunction.Run,
      automationRunData,
      speckleToken,
      new FunctionInputs { ForbiddenSpeckleType = "Base" }
    );

    Assert.That(automationContext.RunStatus, Is.EqualTo("FAILED"));

    var status = await GetAutomationStatus(
      automationRunData.ProjectId,
      automationRunData.ModelId,
      automationContext.SpeckleClient
    );

    Assert.That(status.Status, Is.EqualTo(automationContext.RunStatus));
    var statusMessage = status["automationRuns"][0]["functionRuns"][0]["statusMessage"];

    Assert.That(statusMessage, Is.EqualTo(automationContext.AutomationResult.StatusMessage));
  }

  private static async Task TestFileUploads(AutomationRunData automationRunData, string speckleToken)
  {
    automationRunData = speckleToken = GetSpeckleToken();
    var automationContext = await AutomationContext.Initialize(automationRunData, speckleToken);

    string filePath = $"./{RandomString(10)}";
    await File.WriteAllTextAsync(filePath, "foobar");

    await automationContext.StoreFileResult(filePath);

    File.Delete(filePath);
    Assert.That(automationContext.AutomationResult.Blobs, Has.Count.EqualTo(1));
  }
}

public struct FunctionInputs
{
  [Required]
  public string ForbiddenSpeckleType { get; set; }
}

public static class AutomateFunction
{
  public static async Task Run(AutomationContext automateContext, FunctionInputs functionInputs)
  {
    var versionRootObject = await automateContext.ReceiveVersion();

    int count = 0;
    if (versionRootObject.speckle_type == functionInputs.ForbiddenSpeckleType)
    {
      if (versionRootObject.id is null)
        throw new Exception("Cannot operate on objects without their ids");

      automateContext.AttachErrorToObjects(
        "",
        new[] { versionRootObject.id },
        $"This project should not contain the type: {functionInputs.ForbiddenSpeckleType} "
      );
      count += 1;
    }

    if (count > 0)
    {
      automateContext.MarkRunFailed(
        "Automation failed: "
          + $"Found {count} object that have a forbidden speckle type: {functionInputs.ForbiddenSpeckleType}"
      );
    }
    else
    {
      automateContext.MarkRunSuccess("No forbidden types found.");
    }
  }
}
