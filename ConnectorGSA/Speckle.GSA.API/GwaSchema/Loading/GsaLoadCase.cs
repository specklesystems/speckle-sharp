using System.Collections.Generic;
using System.Linq;

namespace Speckle.GSA.API.GwaSchema
{
  //Named load case instead of load title because it is referred to as such by the documentation at
  //https://www.oasys-software.com/help/gsa/10.1/GSA_Text.html
  [GsaType(GwaKeyword.LOAD_TITLE, GwaSetCommandType.Set, true)]
  public class GsaLoadCase : GsaRecord
  {
    public StructuralLoadCaseType CaseType;
    public string Title;

    public GsaLoadCase() : base()
    {
      //Defaults
      Version = 2;
    }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }
      var items = remainingItems;

      //Only basic level of support is offered now - the arguments after type are ignored
      //LOAD_TITLE.2 | case | title | type | source | category | dir | include | bridge
      //Note: case is deserialised into the Index field
      return FromGwaByFuncs(items, out _, AddTitle, AddType);
    }    

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //LOAD_TITLE.2 | case | title | type | source | category | dir | include | bridge
      //Note: case will be serialised from the Index field
      AddItems(ref items, Title, CaseType.LoadCaseTypeToString(), 1, "~", "NONE", "INC_BOTH");

      gwa = (Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return gwa.Count() > 0;
    }

    #region from_gwa_fns
    public bool AddTitle(string v)
    {
      Title = string.IsNullOrEmpty(v) ? null : v;
      return true;
    }

    public bool AddType(string v)
    {
      CaseType = v.StringToLoadCaseType();
      return true;
    }
    #endregion

  }
}
