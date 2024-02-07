using System.Diagnostics;
using GraphQL;
using Speckle.Automate.Sdk.Schema;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Speckle.Newtonsoft.Json;
using Speckle.Newtonsoft.Json.Serialization;

namespace Speckle.Automate.Sdk;

public class AutomationContext
{
  public AutomationRunData AutomationRunData { get; set; }
  public string? ContextView => AutomationResult.ResultView;
  public Client SpeckleClient { get; private set; }

  private ServerTransport _serverTransport;
  private string _speckleToken;

  // keep a memory transport at hand, to speed up things if needed
  private MemoryTransport _memoryTransport;

  // added for performance measuring
  private Stopwatch _initTime;

  internal AutomationResult AutomationResult { get; private set; }

  public static async Task<AutomationContext> Initialize(AutomationRunData automationRunData, string speckleToken)
  {
    Account account =
      new()
      {
        token = speckleToken,
        serverInfo = new ServerInfo { url = automationRunData.SpeckleServerUrl }
      };
    await account.Validate().ConfigureAwait(false);
    Client client = new(account);
    ServerTransport serverTransport = new(account, automationRunData.ProjectId);
    Stopwatch initTime = new();
    initTime.Start();

    return new AutomationContext
    {
      AutomationRunData = automationRunData,
      SpeckleClient = client,
      _serverTransport = serverTransport,
      _speckleToken = speckleToken,
      _memoryTransport = new MemoryTransport(),
      _initTime = initTime,
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
  public TimeSpan Elapsed => _initTime.Elapsed;

  /// <summary>
  /// Receive version for automation.
  /// </summary>
  /// <returns> Commit object. </returns>
  /// <exception cref="SpeckleException">Throws if commit object is null.</exception>
  public async Task<Base> ReceiveVersion()
  {
    Commit? commit = await SpeckleClient
      .CommitGet(AutomationRunData.ProjectId, AutomationRunData.VersionId)
      .ConfigureAwait(false);
    Base? commitRootObject = await Operations
      .Receive(commit.referencedObject, _serverTransport, _memoryTransport)
      .ConfigureAwait(false);
    if (commitRootObject == null)
    {
      throw new SpeckleException("Commit root object was null");
    }

    Console.WriteLine(
      $"It took {Elapsed.TotalSeconds} seconds to receive the speckle version {AutomationRunData.VersionId}"
    );
    return commitRootObject;
  }

  /// <summary>
  /// Creates new version in the project.
  /// </summary>
  /// <param name="rootObject"> Object to send to project.</param>
  /// <param name="modelName"> Model name to create version in it.</param>
  /// <param name="versionMessage"> Version message.</param>
  /// <returns> Version id.</returns>
  /// <exception cref="ArgumentException"> Throws if given model name is as same as with model name in automation run data.
  /// The reason is to prevent circular run loop in automation. </exception>
  public async Task<string> CreateNewVersionInProject(Base rootObject, string modelName, string versionMessage = "")
  {
    if (modelName == AutomationRunData.BranchName)
    {
      throw new ArgumentException(
        $"The target model: {modelName} cannot match the model that triggered this automation: {AutomationRunData.ModelId}/{AutomationRunData.BranchName}",
        nameof(modelName)
      );
    }

    string rootObjectId = await Operations
      .Send(rootObject, new List<ITransport> { _serverTransport, _memoryTransport })
      .ConfigureAwait(false);

    Branch branch = await SpeckleClient.BranchGet(AutomationRunData.ProjectId, modelName).ConfigureAwait(false);
    if (branch is null)
    {
      // Create the branch with the specified name
      await SpeckleClient
        .BranchCreate(new BranchCreateInput() { streamId = AutomationRunData.ProjectId, name = modelName })
        .ConfigureAwait(false);
    }
    string versionId = await SpeckleClient
      .CommitCreate(
        new CommitCreateInput
        {
          streamId = AutomationRunData.ProjectId,
          branchName = modelName,
          objectId = rootObjectId,
          message = versionMessage,
        }
      )
      .ConfigureAwait(false);
    return versionId;
  }

  /// <summary>
  /// Set context view for automation result view.
  /// </summary>
  /// <param name="resourceIds"> Resource contexts to bind into view.</param>
  /// <param name="includeSourceModelVersion"> Whether bind source version into result view or not.</param>
  /// <exception cref="SpeckleException"> Throws if there is no context to create result view.</exception>
  public void SetContextView(List<string>? resourceIds = null, bool includeSourceModelVersion = true)
  {
    List<string> linkResources = new();
    if (includeSourceModelVersion)
    {
      linkResources.Add($@"{AutomationRunData.ModelId}@{AutomationRunData.VersionId}");
    }

    if (resourceIds is not null)
    {
      linkResources.AddRange(resourceIds);
    }

    if (linkResources.Count == 0)
    {
      throw new SpeckleException("We do not have enough resource ids to compose a context view");
    }

    AutomationResult.ResultView = $"/projects/{AutomationRunData.ProjectId}/models/{string.Join(",", linkResources)}";
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
    GraphQLRequest request =
      new()
      {
        Query =
          @"
            mutation ReportFunctionRunStatus(
                $automationId: String!,
                $automationRevisionId: String!,
                $automationRunId: String!,
                $versionId: String!,
                $functionId: String!,
                $functionName: String!,
                $functionLogo: String,
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
                    functionId: $functionId,
                    functionName: $functionName,
                    functionLogo: $functionLogo,
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
          functionName = AutomationRunData.FunctionName,
          functionLogo = AutomationRunData.FunctionLogo,
          runStatus = RunStatus,
          statusMessage = AutomationResult.StatusMessage,
          elapsed = Elapsed.TotalSeconds,
          resultVersionIds = AutomationResult.ResultVersions,
          objectResults,
        }
      };
    await SpeckleClient.ExecuteGraphQLRequest<Dictionary<string, object>>(request).ConfigureAwait(false);
  }

  /// <summary>
  /// Stores result file in automation result. It will be available to download on Frontend if added.
  /// </summary>
  /// <param name="filePath"> File path to store.</param>
  /// <exception cref="FileNotFoundException"> Throws if given file path is not exist.</exception>
  /// <exception cref="SpeckleException"> Throws if upload requests return no result.</exception>
  public async Task StoreFileResult(string filePath)
  {
    if (!File.Exists(filePath))
    {
      throw new FileNotFoundException("The given file path doesn't exist", fileName: filePath);
    }

    using MultipartFormDataContent formData = new();
    FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read);
    using StreamContent streamContent = new(fileStream);
    formData.Add(streamContent, "files", Path.GetFileName(filePath));
    HttpResponseMessage? request = await SpeckleClient.GQLClient.HttpClient
      .PostAsync(
        new Uri($"{AutomationRunData.SpeckleServerUrl}/api/stream/{AutomationRunData.ProjectId}/blob"),
        formData
      )
      .ConfigureAwait(false);
    request.EnsureSuccessStatusCode();
    string? responseString = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
    Console.WriteLine("RESPONSE - " + responseString);
    BlobUploadResponse uploadResponse = JsonConvert.DeserializeObject<BlobUploadResponse>(responseString);
    if (uploadResponse.UploadResults.Count != 1)
    {
      throw new SpeckleException("Expected one upload result.");
    }

    AutomationResult.Blobs.AddRange(uploadResponse.UploadResults.Select(r => r.BlobId));
  }

  private void MarkRun(AutomationStatus status, string? statusMessage)
  {
    double duration = Elapsed.TotalSeconds;
    AutomationResult.StatusMessage = statusMessage;
    string statusValue = AutomationStatusMapping.Get(status);
    AutomationResult.RunStatus = statusValue;
    AutomationResult.Elapsed = duration;

    string msg = $"Automation run {statusValue} after {duration} seconds.";
    if (statusMessage is not null)
    {
      msg += $"\n{statusMessage}";
    }

    Console.WriteLine(msg);
  }

  public void MarkRunFailed(string statusMessage) => MarkRun(AutomationStatus.Failed, statusMessage);

  public void MarkRunSuccess(string? statusMessage) => MarkRun(AutomationStatus.Succeeded, statusMessage);

  public void AttachErrorToObjects(
    string category,
    IEnumerable<string> objectIds,
    string? message = null,
    Dictionary<string, object>? metadata = null,
    Dictionary<string, object>? visualOverrides = null
  ) => AttachResultToObjects(ObjectResultLevel.Error, category, objectIds, message, metadata, visualOverrides);

  public void AttachWarningToObjects(
    string category,
    IEnumerable<string> objectIds,
    string? message = null,
    Dictionary<string, object>? metadata = null,
    Dictionary<string, object>? visualOverrides = null
  ) => AttachResultToObjects(ObjectResultLevel.Warning, category, objectIds, message, metadata, visualOverrides);

  public void AttachInfoToObjects(
    string category,
    IEnumerable<string> objectIds,
    string? message = null,
    Dictionary<string, object>? metadata = null,
    Dictionary<string, object>? visualOverrides = null
  ) => AttachResultToObjects(ObjectResultLevel.Info, category, objectIds, message, metadata, visualOverrides);

  public void AttachResultToObjects(
    ObjectResultLevel level,
    string category,
    IEnumerable<string> objectIds,
    string? message = null,
    Dictionary<string, object>? metadata = null,
    Dictionary<string, object>? visualOverrides = null
  )
  {
    string levelString = ObjectResultLevelMapping.Get(level);
    List<string> objectIdList = objectIds.ToList();
    Console.WriteLine($"Created new {levelString.ToUpper()} category: {category} caused by: {message}");

    ResultCase resultCase =
      new()
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
