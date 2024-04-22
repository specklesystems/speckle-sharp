using System.Collections.Generic;
using Speckle.Core.Models;

namespace Speckle.Connectors.Utils.Operations;

public struct SendInfo
{
  public string AccountId { get; set; }
  public string ProjectId { get; set; }
  public string ModelId { get; set; }
  public Dictionary<string, ObjectReference> ConvertedObjects { get; set; }
  public HashSet<string> ChangedObjectIds { get; set; }
}
