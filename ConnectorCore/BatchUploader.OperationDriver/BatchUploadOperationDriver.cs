using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.Models.Filters;
using DesktopUI2.ViewModels;
using Speckle.BatchUploader.Sdk;
using Speckle.BatchUploader.Sdk.CommunicationModels;
using Speckle.BatchUploader.Sdk.Interfaces;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;

namespace Speckle.BatchUploader.OperationDriver;

/// <summary>
/// Connector implementation of a BatchUploader client
/// </summary>
/// <remarks>
/// Only processes started by the batch uploader will start executing jobs
/// </remarks>
public sealed class BatchUploadOperationDriver
{
  private readonly BatchUploaderClient _client;
  private readonly IApplicationFunctionalityController _applicationController;
  private readonly ConnectorBindings _connectorBindings;
  private int _jobCounter;

  public BatchUploadOperationDriver(
    BatchUploaderClient client,
    IApplicationFunctionalityController applicationController,
    ConnectorBindings connectorBindings
  )
  {
    _client = client;
    _applicationController = applicationController;
    _connectorBindings = connectorBindings;
  }

  /// <summary>
  /// Queries local BatchUploader service for any jobs for this application process
  /// and starts executing them. Application will terminate once all jobs are complete.
  /// </summary>
  public async Task ProcessAllJobs()
  {
    while (await GetNextJobId().ConfigureAwait(false) is { } jobId)
    {
      // currently using this counter to determine whether this process was started by the batch uploader
      // and needs to be closed by the batch uploader. Not the most robust system, I'd imagine.
      _jobCounter++;
      try
      {
        await ProcessJob(jobId).ConfigureAwait(false);
        await _client.UpdateJobStatus(jobId, JobStatus.Completed).ConfigureAwait(false);
        //TODO: Finish endpoint with result details.
        Console.WriteLine("Finished");
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex);
        //TODO: Finish endpoint with ex details
        await _client.UpdateJobStatus(jobId, JobStatus.Failed).ConfigureAwait(false);
        throw;
      }
    }

    if (_jobCounter > 0)
    {
      Environment.Exit(0);
    }
  }

  private async Task ProcessJob(Guid jobId)
  {
    await _client.UpdateJobStatus(jobId, JobStatus.Processing).ConfigureAwait(false);
    var jobDescription = await _client.GetJobDescription(jobId).ConfigureAwait(false);
    await _applicationController.OpenDocument(jobDescription.FilePath).ConfigureAwait(false);

    Account defaultAccount = AccountManager.GetDefaultAccount() ?? throw new SpeckleException("No default account");

    var state = new StreamState()
    {
      Client = new Client(defaultAccount),
      BranchName = jobDescription.Branch,
      StreamId = jobDescription.Stream,
      Filter = new AllSelectionFilter
      {
        Slug = "all",
        Name = "Everything",
        Icon = "CubeScan",
        Description = "Selects all document objects and project information."
      },
    };
    ProgressViewModel progress = new();
    AddProgressListener(jobId, progress);
    await _connectorBindings.SendStream(state, progress).ConfigureAwait(false);
  }

  private void AddProgressListener(Guid jobId, ProgressViewModel viewModel)
  {
    viewModel.PropertyChanged += (_, p) =>
    {
      if (p.PropertyName is not (nameof(ProgressViewModel.Value) or nameof(ProgressViewModel.Max)))
      {
        return;
      }

      Task.Run(() => OnProgressUpdate(jobId, viewModel));
    };
  }

  private async Task OnProgressUpdate(Guid jobId, ProgressViewModel viewModel)
  {
    //Check for cancel
    var status = await _client.GetJobStatus(jobId).ConfigureAwait(false);
    if (status is JobStatus.Cancelled or JobStatus.Failed)
    {
      viewModel.CancellationTokenSource.Cancel();
    }

    //Update Progress
    JobProgress newProgress = new((long)viewModel.Value, (long)viewModel.Max);
    await _client.UpdateJobProgress(jobId, newProgress).ConfigureAwait(false);
  }

  private async Task<Guid?> GetNextJobId()
  {
    try
    {
      return await _client.GetJob().ConfigureAwait(false);
    }
    catch (HttpRequestException ex)
    {
      Console.WriteLine(ex);
      return null;
    }
  }
}
