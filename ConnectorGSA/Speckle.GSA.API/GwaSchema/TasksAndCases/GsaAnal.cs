using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Speckle.GSA.API.GwaSchema
{
  //This needs review: seems to be a SET keyword but the index is of a load case, not a ANAL index
  [GsaType(GwaKeyword.ANAL, GwaSetCommandType.Set, true, GwaKeyword.LOAD_TITLE, GwaKeyword.TASK)]
  public class GsaAnal : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public int? LoadCase;
    public string Desc;

    public GsaAnal()
    {
      Version = 1;
    }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }

      //ANAL | case | name | task | desc
      return FromGwaByFuncs(remainingItems, out var _, AddName, (v) => AddNullableIndex(v, out LoadCase), (v) => AddStringValue(v, out Desc));
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //ANAL | case | name | task | desc
      AddItems(ref items, Name, LoadCase ?? 0, Desc);

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }
  }
}
