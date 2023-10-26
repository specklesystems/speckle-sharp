# nullable enable
using System.Diagnostics;
using GraphQL;
using Speckle.Automate.Sdk.Schema;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Speckle.Newtonsoft.Json;
using Speckle.Newtonsoft.Json.Serialization;

namespace Speckle.Automate.Sdk;

public class AutomationContext
{
  public AutomationRunData AutomationRunData { get; set; }
  public Client SpeckleClient { get; set; }

  private ServerTransport serverTransport;
  private string speckleToken;

  // keep a memory transport at hand, to speed up things if needed
  private MemoryTransport memoryTransport;

  // added for performance measuring
  private Stopwatch initTime;

  internal AutomationResult AutomationResult { get; set; }

  public static async Task<AutomationContext> Initialize(AutomationRunData automationRunData, string speckleToken)
  {
    var account = new Account
    {
      token = speckleToken,
      serverInfo = new ServerInfo { url = automationRunData.SpeckleServerUrl }
    };
    await account.Validate().ConfigureAwait(false);
    var client = new Client(account);
    var serverTransport = new ServerTransport(account, automationRunData.ProjectId);
    var initTime = new Stopwatch();
    initTime.Start();

    return new AutomationContext
    {
      AutomationRunData = automationRunData,
      SpeckleClient = client,
      serverTransport = serverTransport,
      speckleToken = speckleToken,
      memoryTransport = new MemoryTransport(),
      initTime = initTime,
      AutomationResult = new AutomationResult(),
    };
  }

  public static async Task<AutomationContext> Initialize(string automationRunData, string speckleToken)
  {
    var runData = JsonConvert.DeserializeObject<AutomationRunData>(
      automationRunData,
      new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }
    );
    return await Initialize(runData, speckleToken).ConfigureAwait(false);
  }

  public string RunStatus => AutomationResult.RunStatus;

  public string? StatusMessage => AutomationResult.StatusMessage;
  public TimeSpan Elapsed => initTime.Elapsed;

  public async Task<Base> ReceiveVersion()
  {
    var commit = await SpeckleClient
      .CommitGet(AutomationRunData.ProjectId, AutomationRunData.VersionId)
      .ConfigureAwait(false);
    var commitRootObject = await Operations
      .Receive(commit.referencedObject, serverTransport, memoryTransport)
      .ConfigureAwait(false);
    if (commitRootObject == null)
      throw new Exception("Commit root object was null");
    Console.WriteLine(
      $"It took {Elapsed.TotalSeconds} seconds to receive the speckle version {AutomationRunData.VersionId}"
    );
    return commitRootObject;
  }

  public async Task<string> CreateNewVersionInProject(Base rootObject, string modelId, string versionMessage = "")
  {
    if (modelId == AutomationRunData.ModelId)
      throw new ArgumentException(
        $"The target model id: {modelId} cannot match the model id that triggered this automation: {AutomationRunData.ModelId}",
        nameof(modelId)
      );
    var rootObjectId = await Operations
      .Send(rootObject, new List<ITransport> { serverTransport, memoryTransport }, useDefaultCache: false)
      .ConfigureAwait(false);
    var model = await SpeckleClient.ModelGet(AutomationRunData.ProjectId, modelId).ConfigureAwait(false);
    var versionId = await SpeckleClient
      .CommitCreate(
        new CommitCreateInput
        {
          streamId = AutomationRunData.ProjectId,
          branchName = model.name,
          objectId = rootObjectId,
          message = versionMessage,
        }
      )
      .ConfigureAwait(false);
    return versionId;
  }

  public async Task ReportRunStatus()
  {
    ObjectResults? objectResults = null;
    if (RunStatus is "SUCCEEDED" or "FAILED")
    {
      objectResults = new ObjectResults
      {
        Values = new ObjectResultValues
        {
          BlobIds = AutomationResult.Blobs,
          ObjectResults = AutomationResult.ObjectResults
        }
      };
    }
    var request = new GraphQLRequest
    {
      Query =
        @"
            mutation ReportFunctionRunStatus(
                $automationId: String!,
                $automationRevisionId: String!,
                $automationRunId: String!,
                $versionId: String!,
                $functionId: String!,
                $runStatus: AutomationRunStatus!
                $elapsed: Float!
                $resultVersionIds: [String!]!
                $statusMessage: String
                $objectResults: JSONObject
            ){
              automationMutations {
                functionRunStatusReport(input: {
                  automationId: $automationId
                  automationRevisionId: $automationRevisionId
                  automationRunId: $automationRunId
                  versionId: $versionId
                  functionRuns: [{
                    functionId: $functionId
                    status: $runStatus,
                    elapsed: $elapsed,
                    resultVersionIds: $resultVersionIds,
                    statusMessage: $statusMessage,
                    results: $objectResults,
                  }]
                })
              }
            }
        ",
      Variables = new
      {
        automationId = AutomationRunData.AutomationId,
        automationRevisionId = AutomationRunData.AutomationRevisionId,
        automationRunId = AutomationRunData.AutomationRunId,
        versionId = AutomationRunData.VersionId,
        functionId = AutomationRunData.FunctionId,
        runStatus = RunStatus,
        statusMessage = AutomationResult.StatusMessage,
        elapsed = Elapsed.TotalSeconds,
        resultVersionIds = AutomationResult.ResultVersions,
        objectResults,
      }
    };
    await SpeckleClient.ExecuteGraphQLRequest<Dictionary<string, object>>(request).ConfigureAwait(false);
  }

  public async Task StoreFileResult(string filePath)
  {
    if (!File.Exists(filePath))
      throw new FileNotFoundException("The given file path doesn't exist", fileName: filePath);
    using var formData = new MultipartFormDataContent();

    var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
    using var streamContent = new StreamContent(fileStream);
    formData.Add(streamContent, "files", Path.GetFileName(filePath));
    var request = await SpeckleClient.GQLClient.HttpClient
      .PostAsync(
        new Uri($"{AutomationRunData.SpeckleServerUrl}/api/stream/{AutomationRunData.ProjectId}/blob"),
        formData
      )
      .ConfigureAwait(false);
    request.EnsureSuccessStatusCode();
    var responseString = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
    Console.WriteLine("RESPONSE - " + responseString);
    var uploadResponse = JsonConvert.DeserializeObject<BlobUploadResponse>(responseString);
    if (uploadResponse.UploadResults.Count != 1)
      throw new Exception("Expected one upload result.");
    AutomationResult.Blobs.AddRange(uploadResponse.UploadResults.Select(r => r.BlobId));
  }

  private void _markRun(AutomationStatus status, string? statusMessage)
  {
    var duration = Elapsed.TotalSeconds;
    AutomationResult.StatusMessage = statusMessage;
    var statusValue = AutomationStatusMapping.Get(status);
    AutomationResult.RunStatus = statusValue;
    AutomationResult.Elapsed = duration;

    var msg = $"Automation run {statusValue} after {duration} seconds.";
    if (statusMessage is not null)
      msg += $"\n{statusMessage}";
    Console.WriteLine(msg);
  }

  public void MarkRunFailed(string statusMessage)
  {
    _markRun(AutomationStatus.Failed, statusMessage);
  }

  public void MarkRunSuccess(string? statusMessage)
  {
    _markRun(AutomationStatus.Succeeded, statusMessage);
  }

  public void AttachErrorToObjects(
    string category,
    IEnumerable<string> objectIds,
    string? message = null,
    Dictionary<string, object>? metadata = null,
    Dictionary<string, object>? visualOverrides = null
  )
  {
    AttachResultToObjects(ObjectResultLevel.Error, category, objectIds, message, metadata, visualOverrides);
  }

  public void AttachWarningToObjects(
    string category,
    IEnumerable<string> objectIds,
    string? message = null,
    Dictionary<string, object>? metadata = null,
    Dictionary<string, object>? visualOverrides = null
  )
  {
    AttachResultToObjects(ObjectResultLevel.Warning, category, objectIds, message, metadata, visualOverrides);
  }

  public void AttachInfoToObjects(
    string category,
    IEnumerable<string> objectIds,
    string? message = null,
    Dictionary<string, object>? metadata = null,
    Dictionary<string, object>? visualOverrides = null
  )
  {
    AttachResultToObjects(ObjectResultLevel.Info, category, objectIds, message, metadata, visualOverrides);
  }

  public void AttachResultToObjects(
    ObjectResultLevel level,
    string category,
    IEnumerable<string> objectIds,
    string? message = null,
    Dictionary<string, object>? metadata = null,
    Dictionary<string, object>? visualOverrides = null
  )
  {
    var levelString = ObjectResultLevelMapping.Get(level);
    var objectIdList = objectIds.ToList();
    Console.WriteLine(
      $"Object(s) {string.Join(". ", objectIdList)}, was marked with {levelString.ToUpper()}/{category} cause: {message}"
    );

    var resultCase = new ResultCase
    {
      Category = category,
      Level = levelString,
      ObjectIds = objectIdList,
      Message = message,
      Metadata = metadata,
      VisualOverrides = visualOverrides
    };

    AutomationResult.ObjectResults.Add(resultCase);
  }
}
