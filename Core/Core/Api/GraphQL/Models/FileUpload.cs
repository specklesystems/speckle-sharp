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
  public string branchName { get; set; }
  public string? convertedCommitId { get; set; }
  public DateTime convertedLastUpdate { get; set; }
  public FileUploadConversionStatus convertedStatus { get; set; }
  public string? convertedVersionId { get; set; }
  public string fileName { get; set; }
  public int fileSize { get; set; }
  public string fileType { get; set; }
  public string id { get; set; }
  public Model? model { get; set; }
  public string modelName { get; set; }
  public string projectId { get; set; }
  public string streamId { get; set; }
  public bool uploadComplete { get; set; }
  public DateTime uploadDate { get; set; }
  public string userId { get; set; }
}
