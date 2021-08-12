using Speckle.ConnectorGSA.Proxy.GwaParsers;
using Speckle.GSA.API.GwaSchema;

namespace Speckle.ConnectorGSA.Proxy.Cache
{
  public class GsaCacheRecord
  {
    public GwaKeyword Keyword { get; private set; } //This isn't set in the GsaRecord because that's information kept within the connector

    public GsaRecord_ GsaRecord { get; private set; }
    
    //Note: these booleans can't be merged into one state property because records could be both previous and latest, or only one of them
    public bool Latest { get; set; }
    public bool Previous { get; set; }

    //This one provides a valid value for sending even if there is no SID attached to the GSA record itself
    public string ApplicationId { get => GsaRecord == null ? "" : string.IsNullOrEmpty(GsaRecord.ApplicationId)
        ? Helper.FormatApplicationId(Keyword, GsaRecord.Index.Value) : GsaRecord.ApplicationId; }

    public GsaCacheRecord(GwaKeyword keyword, GsaRecord_ gsaRecord, bool previous = false, bool latest = true)
    {
      Keyword = keyword;
      Latest = latest;
      Previous = previous;
      GsaRecord = gsaRecord;
    }
  }
}
