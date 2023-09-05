using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.Models.Filters;
using DesktopUI2.ViewModels;
using Speckle.BatchUploader.ClientSdk;
using Speckle.BatchUploader.ClientSdk.CommunicationModels;
using Speckle.BatchUploader.ClientSdk.Interfaces;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;

namespace BatchUploadOperationDriver
{
  public class BatchUploadOperationDriver
  {
    private readonly BatchUploaderClient client;
    private readonly Process process;
    private readonly IApplicationFunctionalityController applicationController;
    private readonly ConnectorBindings connectorBindings;
    private int jobCounter;
    public BatchUploadOperationDriver(
      BatchUploaderClient client,
      IApplicationFunctionalityController applicationController, 
      ConnectorBindings connectorBindings
    )
    {
      this.client = client;
      this.process = Process.GetCurrentProcess();
      this.applicationController = applicationController;
      this.connectorBindings = connectorBindings;
    }

    public async Task ProcessAllJobs()
    {
//#if DEBUG
//      Debugger.Launch();
//#endif
      while (await GetNextJobId().ConfigureAwait(false) is Guid jobId)
      {
        // currently using this counter to determine wether this process was started by the batch uploader
        // and needs to be closed by the batch uploader. Not the most robust system, I'd imagine.
        jobCounter++;
        try
        {
          await ProcessJob(client, jobId).ConfigureAwait(false);
          await client.UpdateJobStatus(jobId, JobStatus.Completed).ConfigureAwait(false);
          //TODO: Finish endpoint with result details.
          Console.WriteLine("Finished");
        }
        catch (Exception ex)
        {
          Console.WriteLine(ex);
          //TODO: Finish endpoint with ex details
          await client.UpdateJobStatus(jobId, JobStatus.Failed).ConfigureAwait(false);
          throw;
        }
      }

      if (jobCounter > 0)
      {
        process.Kill();
      }
    }

    private async Task ProcessJob(BatchUploaderClient client, Guid jobId)
    {
      await client.UpdateJobStatus(jobId, JobStatus.Processing).ConfigureAwait(false);
      var jobDescription = await client.GetJobDescription(jobId).ConfigureAwait(false);
      await applicationController.OpenDocument(jobDescription.FilePath).ConfigureAwait(false);

      var state = new StreamState()
      {
        Client = new Client(AccountManager.GetDefaultAccount()),
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
      var progress = new ProgressViewModel();

      await connectorBindings.SendStream(state, progress).ConfigureAwait(false);
    }

    private async Task<Guid?> GetNextJobId()
    {
      try
      {
        return await client.GetJob(process.Id.ToString()).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex);
        return null;
      }
    }
  }
}
