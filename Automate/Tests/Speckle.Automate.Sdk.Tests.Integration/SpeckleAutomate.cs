using Newtonsoft.Json;
using Speckle.Automate.Sdk.Schema;
using Speckle.Automate.Sdk.Schema.Triggers;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
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
    string projectId = await _client.StreamCreate(new() { name = "Automate function e2e test" });
    const string BRANCH_NAME = "main";

    Branch model = await _client.BranchGet(projectId, BRANCH_NAME, 1);
    string modelId = model.id;

    using ServerTransport remoteTransport = new(_client.Account, projectId);
    string rootObjId = await Operations.Send(testObject, remoteTransport, false);

    string versionId = await _client.CommitCreate(
      new()
      {
        streamId = projectId,
        objectId = rootObjId,
        branchName = model.name
      }
    );

    string automationName = Utils.RandomString(10);
    string automationId = Utils.RandomString(10);
    string automationRevisionId = Utils.RandomString(10);

    await Utils.RegisterNewAutomation(projectId, modelId, _client, automationId, automationName, automationRevisionId);

    string automationRunId = Utils.RandomString(10);
    string functionId = Utils.RandomString(10);
    string functionName = "Automation name " + Utils.RandomString(10);
    string functionRelease = Utils.RandomString(10);
    string functionRunId = Utils.RandomString(10);

    var triggers = new List<VersionCreationTrigger> { new(modelId, versionId) };

    return new AutomationRunData
    {
      ProjectId = projectId,
      SpeckleServerUrl = _client.ServerUrl,
      AutomationId = automationId,
      AutomationRunId = automationRunId,
      FunctionRunId = functionRunId,
      Triggers = triggers,
    };
  }

  private VersionCreationTrigger GetVersionCreationTrigger(List<VersionCreationTrigger> triggers)
  {
    if (triggers.FirstOrDefault() is not VersionCreationTrigger trigger)
    {
      throw new Exception("Automation run data contained no valid triggers.");
    }

    return trigger;
  }

  private Client _client;
  private Account _account;

  [OneTimeSetUp]
  public async Task Setup()
  {
    _account = await Fixtures.SeedUser().ConfigureAwait(false);
    _client = new Client(_account);
  }

  [Test]
  [Ignore("currently the function run cannot be integration tested with the server")]
  public async Task TestFunctionRun()
  {
    AutomationRunData automationRunData = await AutomationRunData(Utils.TestObject());
    AutomationContext automationContext = await AutomationRunner.RunFunction(
      TestAutomateFunction.Run,
      automationRunData,
      _account.token,
      new TestFunctionInputs { ForbiddenSpeckleType = "Base" }
    );

    Assert.That(automationContext.RunStatus, Is.EqualTo("FAILED"));

    var trigger = GetVersionCreationTrigger(automationRunData.Triggers);

    AutomationStatus status = await AutomationStatusOperations.Get(
      automationRunData.ProjectId,
      trigger.Payload.ModelId,
      automationContext.SpeckleClient
    );

    Assert.That(status.Status, Is.EqualTo(automationContext.RunStatus));
    string statusMessage = status.AutomationRuns[0].FunctionRuns[0].StatusMessage;

    Assert.That(statusMessage, Is.EqualTo(automationContext.AutomationResult.StatusMessage));
  }

  [Test]
  [Ignore("currently the function run cannot be integration tested with the server")]
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
  [Ignore("currently the function run cannot be integration tested with the server")]
  public async Task TestFileUploads()
  {
    AutomationRunData automationRunData = await AutomationRunData(Utils.TestObject());
    AutomationContext automationContext = await AutomationContext.Initialize(automationRunData, _account.token);

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
  [Ignore("currently the function run cannot be integration tested with the server")]
  public async Task TestCreateVersionInProject()
  {
    AutomationRunData automationRunData = await AutomationRunData(Utils.TestObject());
    AutomationContext automationContext = await AutomationContext.Initialize(automationRunData, _account.token);

    const string BRANCH_NAME = "test-branch";
    const string COMMIT_MSG = "automation test";

    await automationContext.CreateNewVersionInProject(Utils.TestObject(), BRANCH_NAME, COMMIT_MSG);

    Branch branch = await automationContext.SpeckleClient
      .BranchGet(automationRunData.ProjectId, BRANCH_NAME, 1)
      .ConfigureAwait(false);

    Assert.NotNull(branch);
    Assert.That(branch.name, Is.EqualTo(BRANCH_NAME));
    Assert.That(branch.commits.items[0].message, Is.EqualTo(COMMIT_MSG));
  }

  [Test]
  [Ignore("currently the function run cannot be integration tested with the server")]
  public async Task TestCreateVersionInProject_ThrowsErrorForSameModel()
  {
    AutomationRunData automationRunData = await AutomationRunData(Utils.TestObject());
    AutomationContext automationContext = await AutomationContext.Initialize(automationRunData, _account.token);

    var trigger = GetVersionCreationTrigger(automationRunData.Triggers);

    string branchName = trigger.Payload.ModelId;
    const string COMMIT_MSG = "automation test";

    Assert.ThrowsAsync<ArgumentException>(async () =>
    {
      await automationContext.CreateNewVersionInProject(Utils.TestObject(), branchName, COMMIT_MSG);
    });
  }

  [Test]
  [Ignore("currently the function run cannot be integration tested with the server")]
  public async Task TestSetContextView()
  {
    AutomationRunData automationRunData = await AutomationRunData(Utils.TestObject());
    AutomationContext automationContext = await AutomationContext.Initialize(automationRunData, _account.token);

    automationContext.SetContextView();

    var trigger = GetVersionCreationTrigger(automationRunData.Triggers);

    Assert.That(automationContext.AutomationResult.ResultView, Is.Not.Null);
    string originModelView = $"{trigger.Payload.ModelId}@{trigger.Payload.VersionId}";
    Assert.That(automationContext.AutomationResult.ResultView?.EndsWith($"models/{originModelView}"), Is.True);

    await automationContext.ReportRunStatus();
    string dummyContext = "foo@bar";

    automationContext.AutomationResult.ResultView = null;
    automationContext.SetContextView(new List<string> { dummyContext }, true);

    Assert.That(automationContext.AutomationResult.ResultView, Is.Not.Null);
    Assert.That(
      automationContext.AutomationResult.ResultView?.EndsWith($"models/{originModelView},{dummyContext}"),
      Is.True
    );

    await automationContext.ReportRunStatus();

    automationContext.AutomationResult.ResultView = null;
    automationContext.SetContextView(new List<string> { dummyContext }, false);

    Assert.That(automationContext.AutomationResult.ResultView, Is.Not.Null);
    Assert.That(automationContext.AutomationResult.ResultView?.EndsWith($"models/{dummyContext}"), Is.True);

    await automationContext.ReportRunStatus();

    automationContext.AutomationResult.ResultView = null;

    Assert.Throws<SpeckleException>(() =>
    {
      automationContext.SetContextView(null, false);
    });

    await automationContext.ReportRunStatus();
  }

  [Test]
  [Ignore("currently the function run cannot be integration tested with the server")]
  public async Task TestReportRunStatus_Succeeded()
  {
    AutomationRunData automationRunData = await AutomationRunData(Utils.TestObject());
    AutomationContext automationContext = await AutomationContext.Initialize(automationRunData, _account.token);

    Assert.That(automationContext.RunStatus, Is.EqualTo(AutomationStatusMapping.Get(Schema.AutomationStatus.Running)));

    automationContext.MarkRunSuccess("This is a success message");

    Assert.That(
      automationContext.RunStatus,
      Is.EqualTo(AutomationStatusMapping.Get(Schema.AutomationStatus.Succeeded))
    );
  }

  [Test]
  [Ignore("currently the function run cannot be integration tested with the server")]
  public async Task TestReportRunStatus_Failed()
  {
    AutomationRunData automationRunData = await AutomationRunData(Utils.TestObject());
    AutomationContext automationContext = await AutomationContext.Initialize(automationRunData, _account.token);

    Assert.That(automationContext.RunStatus, Is.EqualTo(AutomationStatusMapping.Get(Schema.AutomationStatus.Running)));

    string message = "This is a failure message";
    automationContext.MarkRunFailed(message);

    Assert.That(automationContext.RunStatus, Is.EqualTo(AutomationStatusMapping.Get(Schema.AutomationStatus.Failed)));
    Assert.That(automationContext.StatusMessage, Is.EqualTo(message));
  }

  public void Dispose() => _client.Dispose();
}
