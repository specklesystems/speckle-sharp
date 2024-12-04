#nullable disable

using System;
using Speckle.Core.Api.GraphQL.Enums;

namespace Speckle.Core.Api.GraphQL.Models;

public sealed class FileUpload
{
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
  public bool uploadComplete { get; init; }
  public DateTime uploadDate { get; init; }
  public string userId { get; init; }

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
  public string branchName { get; init; }

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
  public string streamId { get; init; }
}
