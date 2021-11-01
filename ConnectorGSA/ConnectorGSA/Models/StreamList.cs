using System.Collections.Generic;

namespace ConnectorGSA.Models
{
  public class StreamList
  {
    public List<StreamListItem> StreamListItems { get; set; } = new List<StreamListItem>();
    public StreamListItem SeletedStreamListItem { get; set; }
  }
}
