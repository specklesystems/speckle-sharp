using Speckle.GSA.API;
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
      
      if (!FromGwaByFuncs(items, out remainingItems, AddTitle, AddType, v => AddNullableIntValue(v, out record.Source), AddCategory, AddDirection, AddInclude))
      {
        return false;
      }
      if (remainingItems.Count > 0)
      {
        return FromGwaByFuncs(remainingItems, out _, AddBridge);
      }
      return true;
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
      AddItems(ref items, record.Title, LoadCaseTypeToString(record.CaseType), record.Source ?? 0, record.Category.GetStringValue(), 
        AddDirection(), record.Include.GetStringValue());
      if (record.Bridge != null) AddItems(ref items, AddBridge());

      gwa = (Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return gwa.Count() > 0;
    }

    #region to_gwa_fns
    private string AddBridge() => (record.Bridge == true) ? "BRIDGE" : "";
    private string AddDirection() => (record.Direction == AxisDirection3.NotSet) ? "NONE" : record.Direction.ToString();
    #endregion

    #region from_gwa_fns
    private bool AddTitle(string v)
    {
      record.Title = string.IsNullOrEmpty(v) ? null : v;
      return true;
    }

    private bool AddType(string v)
    {
      record.CaseType = StringToLoadCaseType(v);
      return true;
    }

    private bool AddCategory(string v)
    {
      switch (v)
      {
        case "A": record.Category = LoadCategory.Residential; break;
        case "B": record.Category = LoadCategory.Office; break;
        case "C": record.Category = LoadCategory.CongregationArea; break;
        case "D": record.Category = LoadCategory.Shop; break;
        case "E": record.Category = LoadCategory.Storage; break;
        case "F": record.Category = LoadCategory.LightTraffic; break;
        case "G": record.Category = LoadCategory.Traffic; break;
        case "H": record.Category = LoadCategory.Roofs; break;
        case "~": record.Category = LoadCategory.NotSet; break;
        default: return false;
      }
      return true;
    }

    private bool AddDirection(string v)
    {
      switch(v)
      {
        case "X": record.Direction = AxisDirection3.X; break;
        case "Y": record.Direction = AxisDirection3.Y; break;
        case "Z": record.Direction = AxisDirection3.Z; break;
        case "NONE": record.Direction = AxisDirection3.NotSet; break;
        default: return false;
      }
      return true;
    }

    private bool AddInclude(string v)
    {
      switch(v)
      {
        case "INC_UNDEF": record.Include = IncludeOption.Undefined; break;
        case "INC_SUP": record.Include = IncludeOption.Unfavourable; break;
        case "INC_INF": record.Include = IncludeOption.Favourable; break;
        case "INC_BOTH": record.Include = IncludeOption.Both; break;
        default: return false;
      }
      return true;
    }

    private bool AddBridge(string v)
    {
      record.Bridge = !(string.IsNullOrEmpty(v));
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
        case "IMPOSED":
        case "LC_VAR_IMP":
        case "LC_VAR_ROOF": 
          return StructuralLoadCaseType.Live;
        case "WIND":
        case "LC_VAR_WIND":
          return StructuralLoadCaseType.Wind;
        case "SNOW":
        case "LC_VAR_SNOW":
          return StructuralLoadCaseType.Snow;
        case "LC_VAR_RAIN":
          return StructuralLoadCaseType.Rain;
        case "SEISMIC":
        case "LC_EQE_ACC":
        case "LC_EQE_STAT":
        case "LC_EQE_RSA":
          return StructuralLoadCaseType.Earthquake;
        case "LC_PERM_SOIL": return StructuralLoadCaseType.Soil;
        case "LC_VAR_TEMP": return StructuralLoadCaseType.Thermal;
        default:
          //TODO: should more case types be added to enum?
          //LC_UNDEF
          //LC_PERM_EQUIV
          //LC_PRESTRESS
          //LC_VAR_EQUIV
          //LC_ACCIDENTAL
          //NOTIONAL
          //UNDEF
          return StructuralLoadCaseType.Generic;
      }
    }

    private string LoadCaseTypeToString(StructuralLoadCaseType caseType)
    {
      switch (caseType)
      {
        case StructuralLoadCaseType.Dead: return ("LC_PERM_SELF");
        case StructuralLoadCaseType.Live: return ("LC_VAR_IMP");
        case StructuralLoadCaseType.Wind: return ("LC_VAR_WIND");
        case StructuralLoadCaseType.Snow: return ("LC_VAR_SNOW");
        case StructuralLoadCaseType.Rain: return ("LC_VAR_RAIN");
        case StructuralLoadCaseType.Earthquake: return ("LC_EQE_STAT");
        case StructuralLoadCaseType.Soil: return ("LC_PERM_SOIL");
        case StructuralLoadCaseType.Thermal: return ("LC_VAR_TEMP");
        default: return ("LC_UNDEF");
      }
    }
  }
}
