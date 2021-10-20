using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  [GsaType(GwaKeyword.UNIT_DATA, GwaSetCommandType.SetNoIndex, true)]
  public class GsaUnitDataParser : GwaParser<GsaUnitData>
  {
    public GsaUnitDataParser(GsaUnitData gsaUnitData) : base(gsaUnitData) { }

    public GsaUnitDataParser() : base(new GsaUnitData()) { }

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
      
      //UNIT_DATA.1 | option | name | factor
      if (FromGwaByFuncs(items, out _, (v) => v.TryParseStringValue(out record.Option), AddName, (v) => double.TryParse(v, out record.Factor)))
      {
        //Unique for UNIT_DATA records because there is no index (which rules out auto generation using keyword and index) and the Name field
        //is overloaded with meaning, which reasonably enables one to assume its name is unique among its peer records.
        //If that name somehow is null, only then create a GUID.
        record.ApplicationId = (string.IsNullOrEmpty(record.Name)) ? Guid.NewGuid().ToString() : record.Name;
        return true;
      }
      return false;
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      //UNIT_DATA is an unusual one in that it lacks an index after the keyword (so it acts like a SetAt in that respect) but it has no index before either
      var items = new List<string>();
      if (includeSet)
      {
        items.Add("SET");
      }
      items.Add(keyword + "." + record.Version);

      //UNIT_DATA.1 | option | name | factor
      AddItems(ref items, record.Option.GetStringValue(), record.Name, record.Factor);

      gwa = (Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return gwa.Count() > 0;
    }

    #region from_gwa_fns
    protected bool AddName(string v)
    {
      record.Name = (string.IsNullOrEmpty(v)) ? null : v;
      return true;
    }
    #endregion
  }
}
