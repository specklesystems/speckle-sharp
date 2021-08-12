using Speckle.GSA.API.GwaSchema;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  //Named load case instead of load title because it is referred to as such by the documentation at
  //https://www.oasys-software.com/help/gsa/10.1/GSA_Text.html
  [GsaType(GwaKeyword.LOAD_TITLE, GwaSetCommandType.Set, true)]
  public class GsaLoadCaseParser : GwaParser<GsaLoadCase>
  {
    public GsaLoadCaseParser(GsaLoadCase gsaLoadCase) : base(gsaLoadCase)  {  }

    public GsaLoadCaseParser() : base(new GsaLoadCase()) { }

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
      AddItems(ref items, record.Title, LoadCaseTypeToString(record.CaseType), 1, "~", "NONE", "INC_BOTH");

      gwa = (Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return gwa.Count() > 0;
    }

    #region from_gwa_fns
    public bool AddTitle(string v)
    {
      record.Title = string.IsNullOrEmpty(v) ? null : v;
      return true;
    }

    public bool AddType(string v)
    {
      record.CaseType = StringToLoadCaseType(v);
      return true;
    }
    #endregion

    private StructuralLoadCaseType StringToLoadCaseType(string type)
    {
      switch (type)
      {
        case "DEAD":
        case "LC_PERM_SELF":
          return StructuralLoadCaseType.Dead;
        case "LC_VAR_IMP": return StructuralLoadCaseType.Live;
        case "WIND": return StructuralLoadCaseType.Wind;
        case "SNOW": return StructuralLoadCaseType.Snow;
        case "SEISMIC": return StructuralLoadCaseType.Earthquake;
        case "LC_PERM_SOIL": return StructuralLoadCaseType.Soil;
        case "LC_VAR_TEMP": return StructuralLoadCaseType.Thermal;
        default: return StructuralLoadCaseType.Generic;
      }
    }

    private string LoadCaseTypeToString(StructuralLoadCaseType caseType)
    {
      switch (caseType)
      {
        case StructuralLoadCaseType.Dead: return ("LC_PERM_SELF");
        case StructuralLoadCaseType.Live: return ("LC_VAR_IMP");
        case StructuralLoadCaseType.Wind: return ("WIND");
        case StructuralLoadCaseType.Snow: return ("SNOW");
        case StructuralLoadCaseType.Earthquake: return ("SEISMIC");
        case StructuralLoadCaseType.Soil: return ("LC_PERM_SOIL");
        case StructuralLoadCaseType.Thermal: return ("LC_VAR_TEMP");
        default: return ("LC_UNDEF");
      }
    }
  }
}
