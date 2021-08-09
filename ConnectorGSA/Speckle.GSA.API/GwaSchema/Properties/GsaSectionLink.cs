using System;
using System.Collections.Generic;
using System.Linq;



namespace Speckle.GSA.API.GwaSchema
{
  //The term "section component" here is a name applied to both the group as a whole as well as one member of the group, 
  //but the latter is shortened to SectionComp to distinguish them here
  [GsaType(GwaKeyword.SECTION_LINK, GwaSetCommandType.Set, false, true, true)]
  public class SectionLink : GsaSectionComponentBase
  {
    public SectionLink() : base()
    {
      Version = 3;
    }

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

      if (includeRef && !AddItems(ref items, Index ?? 0))
      {
        return false;
      }
      return AddItems(ref items, 0,  0, "DISCRETE",  "RECT",  0, "");
    }
  }

}
