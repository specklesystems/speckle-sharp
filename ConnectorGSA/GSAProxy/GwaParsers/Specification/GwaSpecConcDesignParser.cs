using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  [GsaType(GwaKeyword.SPEC_CONC_DESIGN, GwaSetCommandType.SetNoIndex, true)]
  public class GwaSpecConcDesignParser : GwaParser<GsaSpecConcDesign>
  {
    public GwaSpecConcDesignParser(GsaSpecConcDesign gsaSpecConcDesign) : base(gsaSpecConcDesign) { }

    public GwaSpecConcDesignParser() : base(new GsaSpecConcDesign()) { }

    public override bool FromGwa(string gwa)
    {
      var items = Split(gwa);
      var remainingItems = new List<string>();
      if (items.Count() == 0)
      {
        return false;
      }

      gwaSetCommandType = GwaSetCommandType.SetNoIndex;

      if (items[0].StartsWith("set", StringComparison.InvariantCultureIgnoreCase))
      {
        items.Remove(items[0]);
      }

      if (ParseKeywordVersionSid(items[0]))
      {
        //Remove keyword
        items.Remove(items[0]);
      }
      else
      {
        return false;
      }

      //SPEC_CONC_DESIGN | code | country
      if (FromGwaByFuncs(items, out var reaminingItems, (v) => AddStringValue(v, out record.Code)))
      {
        //SPEC_CONC_DESIGN records have no index or name field, but there should only be one such record the whole model, so the application ID
        //can reasonably in practice be assigned to be equal to the keyword
        record.ApplicationId = GwaKeyword.SPEC_CONC_DESIGN.GetStringValue();

        if (reaminingItems != null && remainingItems.Count > 0)
        {
          return FromGwaByFuncs(remainingItems, out _, (v) => AddStringValue(v, out record.Country));
        }
        return true;
      }
      return false;
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      var items = new List<string>();
      if (includeSet)
      {
        items.Add("SET");
      }
      items.Add(keyword + "." + record.Version);

      //SPEC_CONC_DESIGN | code | country
      AddItems(ref items, record.Code, string.IsNullOrEmpty(record.Country) ? "" : record.Country);

      gwa = (Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return gwa.Count() > 0;
    }

    #region to_gwa_fns

    #endregion

    #region from_gwa_fns

    #endregion
  }
}
