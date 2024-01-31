using System;
using System.Collections.Generic;

namespace Speckle.Core.Api.GraphQL.Models;

public sealed class ModelsTreeItem
{
  public List<ModelsTreeItem> children { get; set; }
  public string fullName { get; set; }
  public bool hasChildren { get; set; }
  public string id { get; set; }
  public Model? model { get; set; }
  public string name { get; set; }
  public DateTime updatedAt { get; set; }
}
