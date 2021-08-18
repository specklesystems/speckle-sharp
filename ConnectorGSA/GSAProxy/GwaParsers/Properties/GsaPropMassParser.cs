using Speckle.GSA.API;
using System.Collections.Generic;
using System.Linq;
using Speckle.GSA.API.GwaSchema;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  [GsaType(GwaKeyword.PROP_MASS, GwaSetCommandType.Set, true)]
  public class GsaPropMassParser : GwaParser<GsaPropMass>
  {
    public GsaPropMassParser(GsaPropMass gsaPropMass) : base(gsaPropMass) { }

    public GsaPropMassParser() : base(new GsaPropMass()) { }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }
      var items = remainingItems;
      FromGwaByFuncs(items, out remainingItems, AddName);

      items = remainingItems.Skip(1).ToList();  //Skip colour

      //PROP_MASS.3 | num | name | colour | mass | Ixx | Iyy | Izz | Ixy | Iyz | Izx | mod { | mod_x | mod_y | mod_z }
      //Zero values are valid for origin, but not for vectors below
      if (!FromGwaByFuncs(items, out remainingItems, (v) => double.TryParse(v, out record.Mass),
        (v) => double.TryParse(v, out record.Ixx), (v) => double.TryParse(v, out record.Iyy), (v) => double.TryParse(v, out record.Izz),
        (v) => double.TryParse(v, out record.Ixy), (v) => double.TryParse(v, out record.Iyz), (v) => double.TryParse(v, out record.Izx),
        (v) => v.TryParseStringValue(out record.Mod), (v) => AddNullableDoubleValue(v.Replace("%", ""), out record.ModXPercentage),
        (v) => AddNullableDoubleValue(v.Replace("%", ""), out record.ModYPercentage), (v) => AddNullableDoubleValue(v.Replace("%", ""), 
        out record.ModZPercentage)))
      {
        return false;
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

      //PROP_MASS.3 | num | name | colour | mass | Ixx | Iyy | Izz | Ixy | Iyz | Izx | mod { | mod_x | mod_y | mod_z }
      AddItems(ref items, record.Name, "NO_RGB", record.Mass, record.Ixx, record.Iyy, record.Izz, record.Ixy, record.Iyz, record.Izx, 
        record.Mod.GetStringValue(), record.ModXPercentage + "%", record.ModYPercentage + "%", record.ModZPercentage + "%");

      gwa = (Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return gwa.Count() > 0;
    }

    protected bool AddName(string v)
    {
      record.Name = (string.IsNullOrEmpty(v)) ? null : v;
      return true;
    }
  }
}
