#nullable disable

using System;

namespace Speckle.Core.Api.GraphQL.Models;

//TODO: This enum isn't explicitly defined in the schema, instead its usages are int typed (But represent an enum)
public enum FileUploadConversionStatus
{
  Queued,
  Processing,
  Success,
  Error,
}

public sealed class FileUpload
{
  public string branchName { get; init; }
  public string convertedCommitId { get; init; }
  public DateTime convertedLastUpdate { get; init; }
  public FileUploadConversionStatus convertedStatus { get; init; }
  public string convertedVersionId { get; init; }
  public string fileName { get; init; }
  public int fileSize { get; init; }
  public string fileType { get; init; }
  public string id { get; init; }
  public Model model { get; init; }
  public string modelName { get; init; }
  public string projectId { get; init; }
  public string streamId { get; init; }
  public bool uploadComplete { get; init; }
  public DateTime uploadDate { get; init; }
  public string userId { get; init; }
}
