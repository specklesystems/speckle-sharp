using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  [GsaType(GwaKeyword.LOAD_2D_THERMAL, GwaSetCommandType.SetAt, true, GwaKeyword.LOAD_TITLE, GwaKeyword.EL, GwaKeyword.MEMB)]
  public class GsaLoad2dThermalParser : GwaParser<GsaLoad2dThermal>
  {
    public GsaLoad2dThermalParser(GsaLoad2dThermal gsaLoad2DThermal) : base(gsaLoad2DThermal) { }

    public GsaLoad2dThermalParser() : base(new GsaLoad2dThermal()) { }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }
      var items = remainingItems;

      //LOAD_2D_THERMAL.2 | name | list | case | type | values(n)
      if (!FromGwaByFuncs(items, out remainingItems, AddName, (v) => AddEntities(v, out record.MemberIndices, out record.ElementIndices), 
        (v) => AddNullableIndex(v, out record.LoadCaseIndex), AddType))
      {
        return false;
      }
      items = remainingItems;

      if (items.Count() > 0)
      {
        record.Values = new List<double>();
        foreach (var item in items)
        {
          if (double.TryParse(item, out double v))
          {
            record.Values.Add(v);
          }
          else
          {
            return false;
          }
        }
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

      //LOAD_2D_THERMAL.2 | name | list | case | type | values(n)
      AddItems(ref items, record.Name, AddEntities(record.MemberIndices, record.ElementIndices), record.LoadCaseIndex ?? 0, record.Type.GetStringValue(), AddValues());

      gwa = (Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return gwa.Count() > 0;
    }

    #region to_gwa_fns
    private string AddValues()
    {
      if (record.Values != null && record.Values.Count() > 0)
      {
        return string.Join("\t", record.Values);
      }
      else
      {
        return "";
      }
    }
    private string AddType()
    {
      switch (record.Type)
      {
        case Load2dThermalType.Uniform:
          return "CONS";
        case Load2dThermalType.Gradient:
          return "DZ";
        case Load2dThermalType.General:
          return "GEN";
        default:
          return "";
      }
    }
    #endregion

    #region from_gwa_fns
    protected bool AddName(string v)
    {
      record.Name = (string.IsNullOrEmpty(v)) ? null : v;
      return true;
    }

    private bool AddType(string v)
    {
      switch (v)
      {
        case "CONS":
          record.Type = Load2dThermalType.Uniform;
          return true;
        case "DZ":
          record.Type = Load2dThermalType.Gradient;
          return true;
        case "GEN":
          record.Type = Load2dThermalType.General;
          return true;
        default:
          return false;
      }
    }
    #endregion
  }
}
