using System;
using System.Collections.Generic;
using System.Linq;



namespace Speckle.GSA.API.GwaSchema
{
  //The term "section component" here is a name applied to both the group as a whole as well as one member of the group, 
  //but the latter is shortened to SectionComp to distinguish them here
  [GsaType(GwaKeyword.SECTION_STEEL, GwaSetCommandType.Set, false, true, true)]
  public class SectionSteel : GsaSectionComponentBase
  {
    //The GWA specifies ref (i.e. record index) and name, but when a SECTION_COMP is inside a SECTION command, 
    //the ref is absent and name is blank (empty string) - so they'll be left out here
    public int? GradeIndex;
    public double? PlasElas;
    public double? NetGross;
    public double? Exposed;
    public double? Beta;
    public SectionSteelSectionType Type;
    public SectionSteelPlateType Plate;
    public bool Locked;


    public SectionSteel() : base()
    {
      Version = 2;
    }

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

      //Assume that if this is embedded into another command (like SECTION) then there is no ref (record index)
      if ((bool)GetType().GetAttribute<GsaType>("SelfContained"))
      {
        if (int.TryParse(items[0], out var foundIndex))
        {
          Index = foundIndex;
          items = items.Skip(1).ToList();
        }
      }

      return (FromGwaByFuncs(items, out _, (v) => AddNullableIndex(v, out GradeIndex),
        (v) => AddNullableDoubleValue(v, out PlasElas), (v) => AddNullableDoubleValue(v, out NetGross),
        (v) => AddNullableDoubleValue(v, out Exposed), (v) => AddNullableDoubleValue(v, out Beta),
        AddLocked, (v) => v.TryParseStringValue(out Type), (v) => v.TryParseStringValue(out Plate)));
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      gwa = (GwaItems(out var items, includeSet) && Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    //Note: the ref argument is missing when the GWA was embedded within a SECTION command, hence the addition of the additional boolean argument
    public override bool GwaItems(out List<string> items, bool includeSet = false, bool includeRef = false)
    {
      items = new List<string>();

      if (includeSet)
      {
        items.Add("SET");
      }
      var sid = FormatSidTags(StreamId, ApplicationId);
      items.Add(keyword + "." + Version + ((string.IsNullOrEmpty(sid)) ? "" : ":" + sid));

      if ((bool)GetType().GetAttribute<GsaType>("SelfContained"))
      {
        items.Add(Index.ToString());
      }

      //SECTION_STEEL | ref | grade | plasElas | netGross | exposed | beta | lock | type | plate
      if (includeRef && !AddItems(ref items, Index ?? 0))
      {
        return false;
      }
      return AddItems(ref items, GradeIndex ?? 0, PlasElas ?? 0, NetGross ?? 0, Exposed ?? 0, Beta ?? 0,
        AddLocked(), Type.GetStringValue(), Plate.GetStringValue());
    }

    #region to_gwa_fns
    private string AddLocked()
    {
      return Locked ? "LOCK" : "NO_LOCK";
    }
    #endregion

    #region from_gwa_fns
    private bool AddLocked(string v)
    {
      Locked = v.Trim().Equals("LOCK", StringComparison.InvariantCultureIgnoreCase);
      return true;
    }
    #endregion
  }

}
