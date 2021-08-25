using Speckle.ConnectorGSA.Proxy.GwaParsers;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;

namespace Speckle.ConnectorGSA.Proxy.Cache
{
  public class GsaCacheRecord
  {
    public Type Type { get; private set; }
    public GsaRecord GsaRecord { get; private set; }
    
    //Note: these booleans can't be merged into one state property because records could be both previous and latest, or only one of them
    public bool Latest { get; set; }
    public bool Previous { get; set; }

    public bool IsAlterable { get => !string.IsNullOrEmpty(GsaRecord.ApplicationId) || GsaRecord.ApplicationId.StartsWith("gsa"); }

    public string GeneratedApplicationId { get; set; }

    //This one provides a valid value for sending even if there is no SID attached to the GSA record itself
    //public string ApplicationId
    //{
    //  get => (GsaRecord == null) ? "" : string.IsNullOrEmpty(GsaRecord.ApplicationId) ? GeneratedApplicationId : GsaRecord.ApplicationId;
    //}
    public string ApplicationId
    {
      get => GsaRecord == null ? "" : string.IsNullOrEmpty(GsaRecord.ApplicationId) 
        ? Helper.FormatApplicationId(Type, GsaRecord.Index.Value) : GsaRecord.ApplicationId;
    }

    public GsaCacheRecord(GsaRecord gsaRecord, bool previous = false, bool latest = true)
    {
      Type = gsaRecord.GetType();
      Latest = latest;
      Previous = previous;
      GsaRecord = gsaRecord;
    }
  }
}
