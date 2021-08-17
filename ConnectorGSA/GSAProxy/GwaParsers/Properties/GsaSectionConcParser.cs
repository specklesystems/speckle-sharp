using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  //The term "section component" here is a name applied to both the group as a whole as well as one member of the group, 
  //but the latter is shortened to SectionComp to distinguish them here
  [GsaType(GwaKeyword.SECTION_CONC, GwaSetCommandType.Set, false, true, true)]
  public class SectionConcParser : GwaParser<SectionConc>, ISectionComponentGwaParser
  {
    //public override Type GsaSchemaType { get => typeof(SectionConc); }

    //The GWA specifies ref (i.e. record index) and name, but when a SECTION_COMP is inside a SECTION command, 
    //the ref is absent and name is blank (empty string) - so they'll be left out here

    public SectionConcParser(SectionConc sectionConc) : base(sectionConc) { }

    public SectionConcParser() : base(new SectionConc()) { }

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
    public  bool GwaItems(out List<string> items, bool includeSet = false, bool includeRef = false)
    {
      items = new List<string>();

      if (includeSet)
      {
        items.Add("SET");
      }
      var sid = FormatSidTags(record.StreamId, record.ApplicationId);
      items.Add(keyword + "." + record.Version + ((string.IsNullOrEmpty(sid)) ? "" : ":" + sid));

      if ((bool)GetType().GetAttribute<GsaType>("SelfContained"))
      {
        items.Add(record.Index.ToString());
      }

      //Documentation says teh following but it's at least 2 arguments short of what is observed from copying GWA
      //SECTION_CONC | ref | grade | agg
      if (includeRef && !AddItems(ref items, record.Index ?? 0))
      {
        return false;
      }
      return AddItems(ref items, 1,  "NO_SLAB", 89.99999998, 0.025, 0);
    }

  }

}
