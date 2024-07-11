using System;
using Speckle.Core.Api.GraphQL.Enums;
using Speckle.Newtonsoft.Json;

namespace Speckle.Core.Api.GraphQL.Models;

public sealed class UserProjectsUpdatedMessage : EventArgs
{
  [JsonRequired]
  public string id { get; init; }

  [JsonRequired]
  public UserProjectsUpdatedMessageType type { get; init; }

  public Project? project { get; init; }
}

public sealed class ProjectCommentsUpdatedMessage : EventArgs
{
  [JsonRequired]
  public string id { get; init; }

  [JsonRequired]
  public ProjectCommentsUpdatedMessageType type { get; init; }

  public Comment? comment { get; init; }
}

public sealed class ProjectFileImportUpdatedMessage : EventArgs
{
  [JsonRequired]
  public string id { get; init; }

  [JsonRequired]
  public ProjectFileImportUpdatedMessageType type { get; init; }

  public FileUpload? upload { get; init; }
}

public sealed class ProjectModelsUpdatedMessage : EventArgs
{
  [JsonRequired]
  public string id { get; init; }

  [JsonRequired]
  public ProjectModelsUpdatedMessageType type { get; init; }

  public Model? model { get; init; }
}

public sealed class ProjectPendingModelsUpdatedMessage : EventArgs
{
  [JsonRequired]
  public string id { get; init; }

  [JsonRequired]
  public ProjectPendingModelsUpdatedMessageType type { get; init; }

  public FileUpload? model { get; init; }
}

public sealed class ProjectUpdatedMessage : EventArgs
{
  [JsonRequired]
  public string id { get; init; }

  [JsonRequired]
  public ProjectUpdatedMessageType type { get; init; }

  public Project? project { get; init; }
}

public sealed class ProjectVersionsUpdatedMessage : EventArgs
{
  [JsonRequired]
  public string id { get; init; }

  [JsonRequired]
  public ProjectVersionsUpdatedMessageType type { get; init; }

  public string? modelId { get; init; }

  public Version? version { get; init; }
}
