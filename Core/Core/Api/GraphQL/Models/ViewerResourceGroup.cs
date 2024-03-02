#nullable disable

using System.Collections.Generic;

namespace Speckle.Core.Api.GraphQL.Models;

public class ViewerResourceGroup
{
  public string identifier { get; init; }
  public List<ViewerResourceItem> items { get; init; }
}
