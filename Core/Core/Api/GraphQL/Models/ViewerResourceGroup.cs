using System.Collections.Generic;

namespace Speckle.Core.Api.GraphQL.Models;

public class ViewerResourceGroup
{
  public string identifier { get; set; }
  public List<ViewerResourceItem> items { get; set; }
}
