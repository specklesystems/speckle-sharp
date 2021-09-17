namespace ConnectorGSA.Models
{
  public class StreamListItem
  {
    private string streamId;
    private string streamName;

    public StreamListItem(string streamId, string streamName)
    {
      this.streamId = streamId;
      this.streamName = streamName;
    }

    public StreamListItem(string streamId)
    {
      this.streamId = streamId;
      this.streamName = null;
    }

    public string StreamName
    {
      get { return streamName; }
      set { streamName = value; }
    }


    public string StreamId
    {
      get { return streamId; }
      set { streamId = value; }
    }

  }
}
