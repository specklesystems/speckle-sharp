using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  //The term "section component" here is a name applied to both the group as a whole as well as one member of the group, 
  //but the latter is shortened to SectionComp to distinguish them here
  [GsaType(GwaKeyword.SECTION_STEEL, GwaSetCommandType.Set, false, true, true)]
  public class SectionSteelParser : GwaParser<SectionSteel>, ISectionComponentGwaParser
  {
    //public override Type GsaSchemaType { get => typeof(SectionSteel); }

    //The GWA specifies ref (i.e. record index) and name, but when a SECTION_COMP is inside a SECTION command, 
    //the ref is absent and name is blank (empty string) - so they'll be left out here
    public SectionSteelParser(SectionSteel sectionSteel) : base(sectionSteel) { }

    public SectionSteelParser() : base(new SectionSteel()) { }

    public override bool FromGwa(string gwa)
    {
      //SECTION_STEEL | ref | grade | plasElas | netGross | exposed | beta | lock | type | plate 
      //Notes: 
      //1. In the documentation, lock is listed at the end but in reality (in 10.1 build 41 at least) it comes before the type parameter
      //2. The ref argument is missing when the GWA was embedded within a SECTION command, so need to detect this case
      //This also means the BasicFromGwa can't be called here because that does assume an index parameter
      var items = Split(gwa);

      if (items[0].StartsWith("set", StringComparison.OrdinalIgnoreCase))
      {
        items.Remove(items[0]);
      }
      if (!ParseKeywordVersionSid(items[0]))
      {
        return false;
      }
      items = items.Skip(1).ToList();

      var record = (SectionSteel)this.record;

      //Assume that if this is embedded into another command (like SECTION) then there is no ref (record index)
      if ((bool)GetType().GetAttribute<GsaType>("SelfContained"))
      {
        if (int.TryParse(items[0], out var foundIndex))
        {
          record.Index = foundIndex;
          items = items.Skip(1).ToList();
        }
      }

      return (FromGwaByFuncs(items, out _, (v) => AddNullableIndex(v, out record.GradeIndex),
        (v) => AddNullableDoubleValue(v, out record.PlasElas), (v) => AddNullableDoubleValue(v, out record.NetGross),
        (v) => AddNullableDoubleValue(v, out record.Exposed), (v) => AddNullableDoubleValue(v, out record.Beta),
        AddLocked, (v) => v.TryParseStringValue(out record.Type), (v) => v.TryParseStringValue(out record.Plate)));
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      gwa = (GwaItems(out var items, includeSet) && Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    //Note: the ref argument is missing when the GWA was embedded within a SECTION command, hence the addition of the additional boolean argument
    public bool GwaItems(out List<string> items, bool includeSet = false, bool includeRef = false)
    {
      items = new List<string>();

      if (includeSet)
      {
        items.Add("SET");
      }
      var record = (SectionSteel)this.record;

      var sid = FormatSidTags(record.StreamId, record.ApplicationId);
      items.Add(keyword + "." + record.Version + ((string.IsNullOrEmpty(sid)) ? "" : ":" + sid));

      if ((bool)GetType().GetAttribute<GsaType>("SelfContained"))
      {
        items.Add(record.Index.ToString());
      }

      //SECTION_STEEL | ref | grade | plasElas | netGross | exposed | beta | lock | type | plate
      if (includeRef && !AddItems(ref items, record.Index ?? 0))
      {
        return false;
      }
      return AddItems(ref items, record.GradeIndex ?? 0, record.PlasElas ?? 0, record.NetGross ?? 0, record.Exposed ?? 0, record.Beta ?? 0,
        AddLocked(), record.Type.GetStringValue(), record.Plate.GetStringValue());
    }

    #region to_gwa_fns
    private string AddLocked()
    {
      return ((SectionSteel)record).Locked ? "LOCK" : "NO_LOCK";
    }
    #endregion

    #region from_gwa_fns
    private bool AddLocked(string v)
    {
      ((SectionSteel)record).Locked = v.Trim().Equals("LOCK", StringComparison.InvariantCultureIgnoreCase);
      return true;
    }
    #endregion
  }

}
