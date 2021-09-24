using Speckle.GSA.API.GwaSchema;
using System.Collections.Generic;
using System.Linq;


namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  [GsaType(GwaKeyword.COMBINATION, GwaSetCommandType.Set, true, GwaKeyword.LOAD_TITLE, GwaKeyword.ANAL)]
  public class GsaCombinationParser : GwaParser<GsaCombination>
  {
    public GsaCombinationParser(GsaCombination gsaCombination) : base(gsaCombination) { }

    public GsaCombinationParser() : base(new GsaCombination()) { }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }

      //COMBINATION | case | name | desc | bridge | note
      if (!FromGwaByFuncs(remainingItems, out remainingItems, AddName, (v) => AddStringValue(v, out record.Desc)))
      {
        return false;
      }
      FromGwaByFuncs(remainingItems, out remainingItems, AddBridge); //Bridge might be blank
      FromGwaByFuncs(remainingItems, out remainingItems, (v) => AddStringValue(v, out record.Note)); //Notes might be blank
      return true;
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //COMBINATION | case | name | desc | bridge | note
      AddItems(ref items, record.Name, record.Desc);
      if (record.Bridge != null) AddItems(ref items, AddBridge());
      if (record.Note != null) AddItems(ref items, record.Note);

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private string AddBridge()
    {
      return (record.Bridge == true) ? "BRIDGE" : "";
    }
    #endregion

    #region from_gwa_fns
    protected bool AddName(string v)
    {
      record.Name = (string.IsNullOrEmpty(v)) ? null : v;
      return true;
    }

    private bool AddBridge(string v)
    {
      record.Bridge = !(string.IsNullOrEmpty(v));
      return true;
    }

    #endregion
  }
}
