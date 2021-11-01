using Speckle.GSA.API.GwaSchema;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  [GsaType(GwaKeyword.AXIS, GwaSetCommandType.Set, true)]
  public class GsaAxisParser : GwaParser<GsaAxis>
  {
    public GsaAxisParser(GsaAxis gsaAxis) : base(gsaAxis) { }

    public GsaAxisParser() : base(new GsaAxis()) { }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }
      var items = remainingItems;

      //AXIS.1 | num | name | type | Ox | Oy | Oz | Xx | Xy | Xz | XYx | XYy | Xyz
      FromGwaByFuncs(items, out remainingItems, AddName);
      items = remainingItems;

      if (!items[0].ToLower().StartsWith("cart"))
      {
        //Only cartesian axes are supported at this stage
        return false;
      }

      items = items.Skip(1).ToList();

      //Zero values are valid for origin, but not for vectors below
      record.OriginX = items[0].ToDouble();
      record.OriginY = items[1].ToDouble();
      record.OriginZ = items[2].ToDouble();
      items = items.Skip(3).ToList();

      //Zero values aren't valid for vectors - so these are to be treated as nullable
      var values = items.Select(i => (double.TryParse(i, out var d) && d > 0) ? (double?)d : null).ToArray();

      if (values.Take(3).Any(v => v.ValidNonZero()))
      {
        record.XDirX = values[0] ?? 0;
        record.XDirY = values[1] ?? 0;
        record.XDirZ = values[2] ?? 0;
      }
      if (values.Skip(3).Take(3).Any(v => v.ValidNonZero()))
      {
        record.XYDirX = values[3] ?? 0;
        record.XYDirY = values[4] ?? 0;
        record.XYDirZ = values[5] ?? 0;
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

      //AXIS.1 | num | name | type | Ox | Oy | Oz | Xx | Xy | Xz | XYx | XYy | Xyz
      AddItems(ref items, record.Name, "CART", record.OriginX, record.OriginY, record.OriginZ, 
        record.XDirX ?? 0, record.XDirY ?? 0, record.XDirZ ?? 0, record.XYDirX ?? 0, record.XYDirY ?? 0, record.XYDirZ ?? 0);

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
