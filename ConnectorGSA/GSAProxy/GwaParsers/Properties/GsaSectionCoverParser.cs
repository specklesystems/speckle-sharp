using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  //The term "section component" here is a name applied to both the group as a whole as well as one member of the group, 
  //but the latter is shortened to SectionComp to distinguish them here
  [GsaType(GwaKeyword.SECTION_COVER, GwaSetCommandType.Set, false, true, true)]
  public class SectionCoverParser : GwaParser<SectionCover>, ISectionComponentGwaParser
  {
    //public override Type GsaSchemaType { get => typeof(SectionCover); }

    //The GWA specifies ref (i.e. record index) and name, but when a SECTION_COMP is inside a SECTION command, 
    //the ref is absent and name is blank (empty string) - so they'll be left out here

    public SectionCoverParser(SectionCover sectionCover) : base(sectionCover) { }

    public SectionCoverParser() : base(new SectionCover()) { }

    public override bool FromGwa(string gwa)
    {
      return true;
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
      var record = (SectionCover)this.record;
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
      return AddItems(ref items, "UNIFORM",  0, 0, "NO_SMEAR");
    }
  }

}
