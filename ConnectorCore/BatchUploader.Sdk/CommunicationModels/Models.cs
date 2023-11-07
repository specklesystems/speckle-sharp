using System.Text.Json.Serialization;

namespace Speckle.BatchUploader.Sdk.CommunicationModels;

public enum JobStatus
{
  None = default,

  /// Waiting for a processor
  Queued,

  /// Job is assigned to a processor
  Assigned,

  /// Processor has started processing Job
  Processing,

  /// User has canceled Job
  Cancelled,

  /// Processor failed to complete Job
  Failed,

  /// Processor successfully completed job
  Completed,
}

public readonly struct JobProgress
{
  public long Progress { get; }
  public long Max { get; }

  [JsonIgnore]
  public double ProgressFraction => (double)Progress / Max;

  [JsonConstructor]
  public JobProgress(long progress, long max)
  {
    Progress = progress;
    Max = max;
  }
}

public readonly struct JobDescription
{
  public string FilePath { get; }
  public string Stream { get; }
  public string Branch { get; }
  public string CommitMessage { get; }

  [JsonConstructor]
  public JobDescription(string filePath, string stream, string branch, string commitMessage)
  {
    FilePath = filePath;
    Stream = stream;
    Branch = branch;
    CommitMessage = commitMessage;
  }
}
