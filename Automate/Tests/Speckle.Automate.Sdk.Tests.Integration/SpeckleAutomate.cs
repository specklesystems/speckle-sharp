using Speckle.Automate.Sdk.Schema;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using TestsIntegration;
using Utils = Speckle.Automate.Sdk.Tests.Integration.TestAutomateUtils;

namespace Speckle.Automate.Sdk.Tests.Integration;

[TestFixture]
public sealed class AutomationContextTest : IDisposable
{
  private async Task<AutomationRunData> AutomationRunData(Base testObject)
  {
    string projectId = await client.StreamCreate(new() { name = "Automate function e2e test" });
    const string branchName = "main";

    Branch model = await client.BranchGet(projectId, branchName, 1);
    string modelId = model.id;

    string rootObjId = await Operations.Send(
      testObject,
      new List<ITransport> { new ServerTransport(client.Account, projectId) }
    );

    string versionId = await client.CommitCreate(
      new()
      {
        streamId = projectId,
        objectId = rootObjId,
        branchName = model.name
      }
    );

    var automationName = Utils.RandomString(10);
    var automationId = Utils.RandomString(10);
    var automationRevisionId = Utils.RandomString(10);

    await Utils.RegisterNewAutomation(projectId, modelId, client, automationId, automationName, automationRevisionId);

    var automationRunId = Utils.RandomString(10);
    var functionId = Utils.RandomString(10);
    var functionName = "Automation name " + Utils.RandomString(10);
    var functionRelease = Utils.RandomString(10);

    return new AutomationRunData
    {
      ProjectId = projectId,
      ModelId = modelId,
      BranchName = branchName,
      VersionId = versionId,
      SpeckleServerUrl = client.ServerUrl,
      AutomationId = automationId,
      AutomationRevisionId = automationRevisionId,
      AutomationRunId = automationRunId,
      FunctionId = functionId,
      FunctionName = functionName,
      FunctionRelease = functionRelease,
    };
  }

  private Client client;
  private Account account;

  [OneTimeSetUp]
  public async Task Setup()
  {
    account = await Fixtures.SeedUser().ConfigureAwait(false);
    client = new Client(account);
  }

  [Test]
  public async Task TestFunctionRun()
  {
    var automationRunData = await AutomationRunData(Utils.TestObject());
    var automationContext = await AutomationRunner.RunFunction(
      TestAutomateFunction.Run,
      automationRunData,
      account.token,
      new TestFunctionInputs { ForbiddenSpeckleType = "Base" }
    );

    Assert.That(automationContext.RunStatus, Is.EqualTo("FAILED"));

    var status = await AutomationStatusOperations.Get(
      automationRunData.ProjectId,
      automationRunData.ModelId,
      automationContext.SpeckleClient
    );

    Assert.That(status.Status, Is.EqualTo(automationContext.RunStatus));
    var statusMessage = status.AutomationRuns[0].FunctionRuns[0].StatusMessage;

    Assert.That(statusMessage, Is.EqualTo(automationContext.AutomationResult.StatusMessage));
  }

  [Test]
  public async Task TestFileUploads()
  {
    var automationRunData = await AutomationRunData(Utils.TestObject());
    var automationContext = await AutomationContext.Initialize(automationRunData, account.token);

    string filePath = $"./{Utils.RandomString(10)}";
    await File.WriteAllTextAsync(filePath, "foobar");
    try
    {
      await automationContext.StoreFileResult(filePath);
    }
    catch (Exception e)
    {
      Console.WriteLine(e);
      throw;
    }

    File.Delete(filePath);
    Assert.That(automationContext.AutomationResult.Blobs, Has.Count.EqualTo(1));
  }

  public void Dispose()
  {
    client.Dispose();
  }
}
