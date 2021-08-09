using System;
using System.Collections.Generic;
using System.Linq;


namespace Speckle.GSA.API.GwaSchema
{
  [GsaType(GwaKeyword.LOAD_2D_THERMAL, GwaSetCommandType.SetAt, true, GwaKeyword.LOAD_TITLE, GwaKeyword.EL, GwaKeyword.MEMB)]
  public class GsaLoad2dThermal : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public List<int> Entities;
    public int? LoadCaseIndex;
    public Load2dThermalType Type;
    public List<double> Values;

    public GsaLoad2dThermal() : base()
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

      //LOAD_2D_THERMAL.2 | name | list | case | type | values(n)
      if (!FromGwaByFuncs(items, out remainingItems, AddName, (v) => AddEntities(v, out Entities), (v) => AddNullableIndex(v, out LoadCaseIndex), AddType))
      {
        return false;
      }
      items = remainingItems;

      if (items.Count() > 0)
      {
        Values = new List<double>();
        foreach (var item in items)
        {
          if (double.TryParse(item, out double v))
          {
            Values.Add(v);
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
      AddItems(ref items, Name, AddEntities(Entities), LoadCaseIndex ?? 0, Type.GetStringValue(), AddValues());

      gwa = (Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return gwa.Count() > 0;
    }

    #region to_gwa_fns
    private string AddValues()
    {
      if (Values != null && Values.Count() > 0)
      {
        return string.Join("\t", Values);
      }
      else
      {
        return "";
      }
    }
    private string AddType()
    {
      switch (Type)
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
    private bool AddType(string v)
    {
      switch (v)
      {
        case "CONS":
          Type = Load2dThermalType.Uniform;
          return true;
        case "DZ":
          Type = Load2dThermalType.Gradient;
          return true;
        case "GEN":
          Type = Load2dThermalType.General;
          return true;
        default:
          return false;
      }
    }
    #endregion
  }
}
