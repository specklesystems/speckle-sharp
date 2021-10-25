using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers.Specification
{
  [GsaType(GwaKeyword.SPEC_STEEL_DESIGN, GwaSetCommandType.SetNoIndex, true)]
  public class GwaSpecSteelDesignParser : GwaParser<GsaSpecSteelDesign>
  {
    public GwaSpecSteelDesignParser(GsaSpecSteelDesign gsaSpecSteelDesign) : base(gsaSpecSteelDesign) { }

    public GwaSpecSteelDesignParser() : base(new GsaSpecSteelDesign()) { }

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

      //SPEC_STEEL_DESIGN | code
      if (FromGwaByFuncs(items, out _, (v) => AddStringValue(v, out record.Code)))
      {
        //SPEC_STEEL_DESIGN records have no index or name field, but there should only be one such record the whole model, so the application ID
        //can reasonably in practice be assigned to be equal to the keyword
        record.ApplicationId = GwaKeyword.SPEC_STEEL_DESIGN.GetStringValue();
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

      //SPEC_STEEL_DESIGN | code
      AddItems(ref items, record.Code);

      gwa = (Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return gwa.Count() > 0;
    }

    #region to_gwa_fns

    #endregion

    #region from_gwa_fns

    #endregion

  }
}
