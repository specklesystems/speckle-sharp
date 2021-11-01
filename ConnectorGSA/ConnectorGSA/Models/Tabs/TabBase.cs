using Speckle.GSA.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConnectorGSA.Models
{
  public abstract class TabBase
  {
    public GSALayer TargetLayer { get; set; }
    public StreamMethod StreamMethod { get; set; } = StreamMethod.Single;
    public StreamList StreamList { get; set; } = new StreamList();

    protected List<StreamState> StreamStates = new List<StreamState>();

    public double PollingRateMilliseconds { get; set; } = 2000;
    public TabBase(GSALayer defaultLayer)
    {
      TargetLayer = defaultLayer;
    }

    public async Task<bool> RefreshStream(string streamId, IProgress<MessageEventArgs> loggingProgress)
    {
      var matching = StreamStates.FirstOrDefault(r => r.StreamId.Equals(streamId, StringComparison.InvariantCultureIgnoreCase));
      if (matching == null)
      {
        return false;
      }
      var refreshed = await matching.RefreshStream(loggingProgress);
      return refreshed;
    }

    public bool StreamStatesToStreamList()
    {
      StreamList.SeletedStreamListItem = null;
      StreamList.StreamListItems.Clear();
      foreach (var sidr in StreamStates)
      {
        StreamList.StreamListItems.Add(new StreamListItem(sidr.StreamId, sidr.Stream.name));
      }
      return true;
    }

    public bool RemoveStreamState(StreamState r)
    {
      var matching = StreamStates.Where(ssr => ssr.StreamId.Equals(r.StreamId, System.StringComparison.InvariantCultureIgnoreCase)).ToList();
      if (matching.Count > 0)
      {
        var indices = matching.Select(m => StreamStates.IndexOf(m)).OrderByDescending(i => i).ToList();
        foreach (var i in indices)
        {
          StreamStates.RemoveAt(i);
        }
      }
      return true;
    }
  }
}
