using System;
using System.Collections.Generic;
using System.Linq;


namespace Speckle.GSA.API.GwaSchema
{
  //Check when implementing: is TASK truly a referenced keyword?
  [GsaType(GwaKeyword.COMBINATION, GwaSetCommandType.Set, true, GwaKeyword.LOAD_TITLE, GwaKeyword.TASK)]
  public class GsaCombination : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public string Desc;
    public bool? Bridge;
    public string Note;

    public GsaCombination()
    {
      Version = 1;
    }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }

      //COMBINATION | case | name | desc | bridge | note
      if (!FromGwaByFuncs(remainingItems, out remainingItems, AddName, (v) => AddStringValue(v, out Desc)))
      {
        return false;
      }
      FromGwaByFuncs(remainingItems, out remainingItems, AddBridge); //Bridge might be blank
      FromGwaByFuncs(remainingItems, out remainingItems, (v) => AddStringValue(v, out Note)); //Notes might be blank
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
      AddItems(ref items, Name, Desc);
      if (Bridge != null) AddItems(ref items, AddBridge());
      if (Note != null) AddItems(ref items, Note);

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private string AddBridge()
    {
      return (Bridge == true) ? "BRIDGE" : "";
    }
    #endregion

    #region from_gwa_fns
    private bool AddBridge(string v)
    {
      Bridge = !(string.IsNullOrEmpty(v));
      return true;
    }

    #endregion
  }
}
