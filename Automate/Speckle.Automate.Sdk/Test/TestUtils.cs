using GraphQL;
using Speckle.Automate.Sdk.Schema;
using Speckle.Automate.Sdk.Schema.Triggers;
using Speckle.Core.Api;

namespace Speckle.Automate.Sdk.Test;

public static class TestAutomateUtils
{
  public static async Task<AutomationRunData> CreateTestRun(Client speckleClient)
  {
    GraphQLRequest query =
      new(
        query: """
                    mutation Mutation($projectId: ID!, $automationId: ID!) {
                        projectMutations {
                            automationMutations(projectId: $projectId) {
                                createTestAutomationRun(automationId: $automationId) {
                                    automationRunId
                                    functionRunId
                                    triggers {
                                        payload {
                                            modelId
                                            versionId
                                        }
                                        triggerType
                                    }
                                }
                            }
                        }
                    }
                """,
        variables: new
        {
          automationId = TestEnvironment.GetSpeckleAutomationId(),
          projectId = TestEnvironment.GetSpeckleProjectId()
        }
      );

    dynamic res = await speckleClient.ExecuteGraphQLRequest<object>(query).ConfigureAwait(false);

    var runData = res["projectMutations"]["automationMutations"]["createTestAutomationRun"];
    var triggerData = runData["triggers"][0]["payload"];

    string modelId = triggerData["modelId"];
    string versionId = triggerData["versionId"];

    var data = new AutomationRunData()
    {
      ProjectId = TestEnvironment.GetSpeckleProjectId(),
      SpeckleServerUrl = TestEnvironment.GetSpeckleServerUrl(),
      AutomationId = TestEnvironment.GetSpeckleAutomationId(),
      AutomationRunId = runData["automationRunId"],
      FunctionRunId = runData["functionRunId"],
      Triggers = new List<AutomationRunTriggerBase>()
      {
        new VersionCreationTrigger(modelId: modelId, versionId: versionId)
      }
    };

    return data;
  }
}
