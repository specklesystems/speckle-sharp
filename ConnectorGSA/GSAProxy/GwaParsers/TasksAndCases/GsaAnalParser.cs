using Speckle.GSA.API.GwaSchema;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  //This needs review: seems to be a SET keyword but the index is of a load case, not a ANAL index
  [GsaType(GwaKeyword.ANAL, GwaSetCommandType.Set, true, GwaKeyword.LOAD_TITLE)]
  public class GsaAnalParser : GwaParser<GsaAnal>
  {
    public GsaAnalParser(GsaAnal gsaAnal) : base(gsaAnal) { }

    public GsaAnalParser() : base(new GsaAnal()) { }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }

      //ANAL | case | name | task | desc
      return FromGwaByFuncs(remainingItems, out var _, AddName, (v) => AddNullableIndex(v, out record.TaskIndex), (v) => AddStringValue(v, out record.Desc));
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //ANAL | case | name | task | desc
      AddItems(ref items, record.Name, record.TaskIndex ?? 0, record.Desc);

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
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
