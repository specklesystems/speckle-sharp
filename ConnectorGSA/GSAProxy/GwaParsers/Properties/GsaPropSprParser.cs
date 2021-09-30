using Speckle.GSA.API.GwaSchema;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  [GsaType(GwaKeyword.PROP_SPR, GwaSetCommandType.Set, true)]
  public class GsaPropSprParser : GwaParser<GsaPropSpr>
  {
    public GsaPropSprParser(GsaPropSpr gsaPropSpr) : base(gsaPropSpr) { }

    public GsaPropSprParser() : base(new GsaPropSpr()) { }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }
      var items = remainingItems;

      //PROP_SPR.4 | num | name | colour | SPRING | curve_x | stiff_x | curve_y | stiff_y | curve_z | stiff_z | curve_xx | stiff_xx | curve_yy | stiff_yy | curve_zz | stiff_zz | damping
      //PROP_SPR.4 | num | name | colour | DAMPER | damping_x | damping_y | damping_z | damping_xx | damping_yy | damping_zz
      //PROP_SPR.4 | num | name | colour | MATRIX | matrix | damping
      //PROP_SPR.4 | num | name | colour | FRICTION | stiff_x | stiff_y | stiff_z | friction | damping
      //PROP_SPR.4 | num | name | colour | type | stiff_x | damping | +ve_lock_up | -ve_lock_up
      FromGwaByFuncs(items, out remainingItems, AddName, null, (v) => EnumParse(v, out record.PropertyType)); //Skip colour
      items = remainingItems;

      var readParams = false;
      record.Stiffnesses = new Dictionary<AxisDirection6, double>();

      if (record.PropertyType == StructuralSpringPropertyType.Friction)
      {
        double x = 0;
        double y = 0;
        double z = 0;
        readParams = FromGwaByFuncs(items, out remainingItems, (v) => double.TryParse(v, out x), (v) => double.TryParse(v, out y),
          (v) => double.TryParse(v, out z), (v) => AddNullableDoubleValue(v, out record.FrictionCoeff), 
          (v) => AddNullableDoubleValue(v, out record.DampingRatio));
        if (readParams)
        {
          record.Stiffnesses.Add(AxisDirection6.X, x);
          record.Stiffnesses.Add(AxisDirection6.Y, y);
          record.Stiffnesses.Add(AxisDirection6.Z, z);
        }
      }
      else if (record.PropertyType == StructuralSpringPropertyType.General)
      {
        double x = 0;
        double y = 0;
        double z = 0;
        double xx = 0;
        double yy = 0;
        double zz = 0;
        readParams = FromGwaByFuncs(items, out remainingItems,
          null, (v) => double.TryParse(v, out x), null, (v) => double.TryParse(v, out y), null, (v) => double.TryParse(v, out z),
          null, (v) => double.TryParse(v, out xx), null, (v) => double.TryParse(v, out yy), null, (v) => double.TryParse(v, out zz),
          (v) => AddNullableDoubleValue(v, out record.DampingRatio));
        if (readParams)
        {
          record.Stiffnesses.Add(AxisDirection6.X, x);
          record.Stiffnesses.Add(AxisDirection6.Y, y);
          record.Stiffnesses.Add(AxisDirection6.Z, z);
          record.Stiffnesses.Add(AxisDirection6.XX, xx);
          record.Stiffnesses.Add(AxisDirection6.YY, yy);
          record.Stiffnesses.Add(AxisDirection6.ZZ, zz);
        }
      }
      else if (record.PropertyType == StructuralSpringPropertyType.Torsional)
      {
        double xx = 0;
        readParams = FromGwaByFuncs(items, out remainingItems, (v) => double.TryParse(v, out xx), (v) => AddNullableDoubleValue(v, out record.DampingRatio));
        if (readParams)
        {
          record.Stiffnesses.Add(AxisDirection6.XX, xx);
        }
      }
      else
      {
        double x = 0;
        readParams = FromGwaByFuncs(items, out remainingItems, (v) => double.TryParse(v, out x), (v) => AddNullableDoubleValue(v, out record.DampingRatio));
        if (readParams)
        {
          record.Stiffnesses.Add(AxisDirection6.X, x);
        }
      }

      if (!readParams)
      {
        record.Stiffnesses = null;
        return false;
      }

      return readParams;
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //PROP_SPR.4 | num | name | colour | SPRING | curve_x | stiff_x | curve_y | stiff_y | curve_z | stiff_z | curve_xx | stiff_xx | curve_yy | stiff_yy | curve_zz | stiff_zz | damping
      AddItems(ref items, record.Name, "NO_RGB", record.PropertyType.ToString().ToUpperInvariant());

      if (record.PropertyType == StructuralSpringPropertyType.Friction)
      {
        AddItems(ref items, GetStiffness(AxisDirection6.X), GetStiffness(AxisDirection6.Y), GetStiffness(AxisDirection6.Z), 
          record.FrictionCoeff, record.DampingRatio);
      }
      else if (record.PropertyType == StructuralSpringPropertyType.General)
      {
        AddItems(ref items, 0, GetStiffness(AxisDirection6.X), 0, GetStiffness(AxisDirection6.Y), 0, GetStiffness(AxisDirection6.Z),
          0, GetStiffness(AxisDirection6.XX), 0, GetStiffness(AxisDirection6.YY), 0, GetStiffness(AxisDirection6.ZZ), record.DampingRatio);
      }
      else if (record.PropertyType == StructuralSpringPropertyType.Torsional)
      {
        AddItems(ref items, GetStiffness(AxisDirection6.XX), record.DampingRatio);
      }
      else if (record.PropertyType == StructuralSpringPropertyType.Lockup)
      {
        AddItems(ref items, GetStiffness(AxisDirection6.X), record.DampingRatio, 0, 0);
      }
      else
      {
        AddItems(ref items, GetStiffness(AxisDirection6.X), record.DampingRatio);
      }

      gwa = (Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return gwa.Count() > 0;
    }

    protected bool AddName(string v)
    {
      record.Name = (string.IsNullOrEmpty(v)) ? null : v;
      return true;
    }

    private double GetStiffness(AxisDirection6 dir)
    {
      return (record.Stiffnesses != null && record.Stiffnesses.ContainsKey(dir)) ? record.Stiffnesses[dir] : 0;
    }
  }
}
