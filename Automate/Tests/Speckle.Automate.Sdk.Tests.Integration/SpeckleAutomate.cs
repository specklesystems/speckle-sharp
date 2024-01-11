using Newtonsoft.Json;
using Speckle.Automate.Sdk.Schema;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Tests.Integration;
using Speckle.Core.Transports;
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

    using ServerTransport remoteTransport = new(client.Account, projectId);
    string rootObjId = await Operations.Send(testObject, remoteTransport, false);

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
  public void TestParseInputData()
  {
    TestFunctionInputs testFunctionInputs = new() { ForbiddenSpeckleType = "Base" };
    FunctionRunData<TestFunctionInputs> functionRunData = new() { FunctionInputs = testFunctionInputs };
    string serializedFunctionRunData = JsonConvert.SerializeObject(functionRunData);
    File.WriteAllText("./inputData.json", serializedFunctionRunData);
    FunctionRunData<TestFunctionInputs>? data = FunctionRunDataParser.FromPath<TestFunctionInputs>("./inputData.json");

    Assert.AreEqual("Base", data.FunctionInputs.ForbiddenSpeckleType);
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

  [Test]
  public async Task TestCreateVersionInProject()
  {
    var automationRunData = await AutomationRunData(Utils.TestObject());
    var automationContext = await AutomationContext.Initialize(automationRunData, account.token);

    const string branchName = "test-branch";
    const string commitMsg = "automation test";

    await automationContext.CreateNewVersionInProject(Utils.TestObject(), branchName, commitMsg);

    var branch = await automationContext.SpeckleClient
      .BranchGet(automationRunData.ProjectId, branchName, 1)
      .ConfigureAwait(false);

    Assert.NotNull(branch);
    Assert.That(branch.name, Is.EqualTo(branchName));
    Assert.That(branch.commits.items[0].message, Is.EqualTo(commitMsg));
  }

  [Test]
  public async Task TestCreateVersionInProject_ThrowsErrorForSameModel()
  {
    var automationRunData = await AutomationRunData(Utils.TestObject());
    var automationContext = await AutomationContext.Initialize(automationRunData, account.token);

    var branchName = automationRunData.BranchName;
    const string commitMsg = "automation test";

    Assert.ThrowsAsync<ArgumentException>(async () =>
    {
      await automationContext.CreateNewVersionInProject(Utils.TestObject(), branchName, commitMsg);
    });
  }

  [Test]
  public async Task TestSetContextView()
  {
    var automationRunData = await AutomationRunData(Utils.TestObject());
    var automationContext = await AutomationContext.Initialize(automationRunData, account.token);

    automationContext.SetContextView();

    Assert.That(automationContext.AutomationResult.ResultView, Is.Not.Null);
    string originModelView = $"{automationRunData.ModelId}@{automationRunData.VersionId}";
    Assert.That(automationContext.AutomationResult.ResultView.EndsWith($"models/{originModelView}"), Is.True);

    await automationContext.ReportRunStatus();
    var dummyContext = "foo@bar";

    automationContext.AutomationResult.ResultView = null;
    automationContext.SetContextView(new List<string> { dummyContext }, true);

    Assert.That(automationContext.AutomationResult.ResultView, Is.Not.Null);
    Assert.That(
      automationContext.AutomationResult.ResultView.EndsWith($"models/{originModelView},{dummyContext}"),
      Is.True
    );

    await automationContext.ReportRunStatus();

    automationContext.AutomationResult.ResultView = null;
    automationContext.SetContextView(new List<string> { dummyContext }, false);

    Assert.That(automationContext.AutomationResult.ResultView, Is.Not.Null);
    Assert.That(automationContext.AutomationResult.ResultView.EndsWith($"models/{dummyContext}"), Is.True);

    await automationContext.ReportRunStatus();

    automationContext.AutomationResult.ResultView = null;

    Assert.Throws<Exception>(() =>
    {
      automationContext.SetContextView(null, false);
    });

    await automationContext.ReportRunStatus();
  }

  [Test]
  public async Task TestReportRunStatus_Succeeded()
  {
    var automationRunData = await AutomationRunData(Utils.TestObject());
    var automationContext = await AutomationContext.Initialize(automationRunData, account.token);

    Assert.That(automationContext.RunStatus, Is.EqualTo(AutomationStatusMapping.Get(Schema.AutomationStatus.Running)));

    automationContext.MarkRunSuccess("This is a success message");

    Assert.That(
      automationContext.RunStatus,
      Is.EqualTo(AutomationStatusMapping.Get(Schema.AutomationStatus.Succeeded))
    );
  }

  [Test]
  public async Task TestReportRunStatus_Failed()
  {
    var automationRunData = await AutomationRunData(Utils.TestObject());
    var automationContext = await AutomationContext.Initialize(automationRunData, account.token);

    Assert.That(automationContext.RunStatus, Is.EqualTo(AutomationStatusMapping.Get(Schema.AutomationStatus.Running)));

    var message = "This is a failure message";
    automationContext.MarkRunFailed(message);

    Assert.That(automationContext.RunStatus, Is.EqualTo(AutomationStatusMapping.Get(Schema.AutomationStatus.Failed)));
    Assert.That(automationContext.StatusMessage, Is.EqualTo(message));
  }

  public void Dispose()
  {
    client.Dispose();
  }
}
