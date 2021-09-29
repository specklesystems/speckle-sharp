using Speckle.GSA.API;
using System.Collections.Generic;
using System.Linq;

namespace ConnectorGSA.Models
{
  public abstract class TabBase
  {
    public GSALayer TargetLayer { get; set; }
    public StreamMethod StreamMethod { get; set; } = StreamMethod.Single;
    public StreamList StreamList { get; set; } = new StreamList();

    protected List<StreamState> sidSpeckleRecords = new List<StreamState>();

    public double PollingRateMilliseconds { get; set; } = 2000;
    public TabBase(GSALayer defaultLayer)
    {
      TargetLayer = defaultLayer;
    }

    public bool ChangeSidRecordStreamName(string streamId, string streamName)
    {
      var matching = sidSpeckleRecords.FirstOrDefault(r => r.StreamId.Equals(streamId, System.StringComparison.InvariantCultureIgnoreCase));
      if (matching == null)
      {
        return false;
      }
      matching.SetName(streamName);
      return true;
    }

    public bool SidRecordsToStreamList()
    {
      StreamList.SeletedStreamListItem = null;
      StreamList.StreamListItems.Clear();
      foreach (var sidr in sidSpeckleRecords)
      {
        StreamList.StreamListItems.Add(new StreamListItem(sidr.StreamId, sidr.Stream.name));
      }
      return true;
    }

    public void StreamListToSidRecords()
    {
      sidSpeckleRecords = StreamList.StreamListItems.Select(sli => new StreamState(sli.StreamId, sli.StreamName)).ToList();
    }

    public bool RemoveSidSpeckleRecord(StreamState r)
    {
      var matching = sidSpeckleRecords.Where(ssr => ssr.StreamId.Equals(r.StreamId, System.StringComparison.InvariantCultureIgnoreCase)).ToList();
      if (matching.Count > 0)
      {
        var indices = matching.Select(m => sidSpeckleRecords.IndexOf(m)).OrderByDescending(i => i).ToList();
        foreach (var i in indices)
        {
          sidSpeckleRecords.RemoveAt(i);
        }
      }
      return true;
    }
  }
}
