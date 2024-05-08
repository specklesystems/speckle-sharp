using System.Collections.Generic;
using Speckle.Core.Models;

namespace Speckle.Connectors.Utils.Operations;

public readonly struct SendInfo
{
  public SendInfo(
    string accountId,
    string projectId,
    string modelId,
    string sourceApplication,
    IReadOnlyDictionary<string, ObjectReference> convertedObjects,
    ISet<string> changedObjectIds
  )
  {
    AccountId = accountId;
    ProjectId = projectId;
    ModelId = modelId;
    SourceApplication = sourceApplication;
    ConvertedObjects = convertedObjects;
    ChangedObjectIds = changedObjectIds;
  }

  public string AccountId { get; }
  public string ProjectId { get; }
  public string ModelId { get; }
  public string SourceApplication { get; }
  public IReadOnlyDictionary<string, ObjectReference> ConvertedObjects { get; }

  public ISet<string> ChangedObjectIds { get; }
}
