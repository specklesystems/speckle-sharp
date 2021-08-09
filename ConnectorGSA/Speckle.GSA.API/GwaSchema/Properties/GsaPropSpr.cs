using System.Collections.Generic;
using System.Linq;

namespace Speckle.GSA.API.GwaSchema
{
  [GsaType(GwaKeyword.PROP_SPR, GwaSetCommandType.Set, true)]
  public class GsaPropSpr : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public Colour Colour = Colour.NO_RGB;
    public StructuralSpringPropertyType PropertyType;
    public Dictionary<AxisDirection6, double> Stiffnesses;
    public double? FrictionCoeff;
    public double? DampingRatio;
    //For GENERAL, there is the option of non-linear curves, but this isn't supported yet
    //Also for LOCKUP, there are positive and negative parameters, but these aren't supported yet either

    public GsaPropSpr() : base()
    {
      //Defaults
      Version = 4;
    }

    public override bool FromGwa(string gwa)
    {
      if(!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }
      var items = remainingItems;

      //PROP_SPR.4 | num | name | colour | SPRING | curve_x | stiff_x | curve_y | stiff_y | curve_z | stiff_z | curve_xx | stiff_xx | curve_yy | stiff_yy | curve_zz | stiff_zz | damping
      FromGwaByFuncs(items, out remainingItems, AddName, null, (v) => EnumParse(v, out PropertyType)); //Skip colour
      items = remainingItems;
      
      var readParams = false;
      Stiffnesses = new Dictionary<AxisDirection6, double>();
      
      if (PropertyType == StructuralSpringPropertyType.Friction)
      {
        double x = 0;
        double y = 0;
        double z = 0;
        readParams = FromGwaByFuncs(items, out remainingItems, (v) => double.TryParse(v, out x), (v) => double.TryParse(v, out y),
          (v) => double.TryParse(v, out z), (v) => AddNullableDoubleValue(v, out FrictionCoeff), (v) => AddNullableDoubleValue(v, out DampingRatio));
        if (readParams)
        {
          Stiffnesses.Add(AxisDirection6.X, x);
          Stiffnesses.Add(AxisDirection6.Y, y);
          Stiffnesses.Add(AxisDirection6.Z, z);
        }
      }
      else if (PropertyType == StructuralSpringPropertyType.General)
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
          (v) => AddNullableDoubleValue(v, out DampingRatio));
        if (readParams)
        {
          Stiffnesses.Add(AxisDirection6.X, x);
          Stiffnesses.Add(AxisDirection6.Y, y);
          Stiffnesses.Add(AxisDirection6.Z, z);
          Stiffnesses.Add(AxisDirection6.XX, xx);
          Stiffnesses.Add(AxisDirection6.YY, yy);
          Stiffnesses.Add(AxisDirection6.ZZ, zz);
        }
      }
      else if (PropertyType == StructuralSpringPropertyType.Torsional)
      {
        double xx = 0;
        readParams = FromGwaByFuncs(items, out remainingItems, (v) => double.TryParse(v, out xx), (v) => AddNullableDoubleValue(v, out DampingRatio));
        if (readParams)
        {
          Stiffnesses.Add(AxisDirection6.XX, xx);
        }
      }
      else
      {
        double x = 0;
        readParams = FromGwaByFuncs(items, out remainingItems, (v) => double.TryParse(v, out x), (v) => AddNullableDoubleValue(v, out DampingRatio));
        if (readParams)
        {
          Stiffnesses.Add(AxisDirection6.X, x);
        }
      }

      if (!readParams)
      {
        Stiffnesses = null;
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
      AddItems(ref items, Name, "NO_RGB", PropertyType.ToString().ToUpperInvariant());

      if (PropertyType == StructuralSpringPropertyType.Friction)
      {
        AddItems(ref items, GetStiffness(AxisDirection6.X), GetStiffness(AxisDirection6.Y), GetStiffness(AxisDirection6.Z), FrictionCoeff, DampingRatio);
      }
      else if (PropertyType == StructuralSpringPropertyType.General)
      {
        AddItems(ref items, 0, GetStiffness(AxisDirection6.X), 0, GetStiffness(AxisDirection6.Y), 0, GetStiffness(AxisDirection6.Z), 
          0, GetStiffness(AxisDirection6.XX), 0, GetStiffness(AxisDirection6.YY), 0, GetStiffness(AxisDirection6.ZZ), DampingRatio);
      }
      else if (PropertyType == StructuralSpringPropertyType.Torsional)
      {
        AddItems(ref items, GetStiffness(AxisDirection6.XX), DampingRatio);
      }
      else if (PropertyType == StructuralSpringPropertyType.Lockup)
      {
        AddItems(ref items, GetStiffness(AxisDirection6.X), DampingRatio, 0, 0);
      }
      else
      {
        AddItems(ref items, GetStiffness(AxisDirection6.X), DampingRatio);
      }

      gwa = (Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return gwa.Count() > 0;
    }

    private double GetStiffness(AxisDirection6 dir)
    {
      return (Stiffnesses != null && Stiffnesses.ContainsKey(dir)) ? Stiffnesses[dir] : 0;
    }
  }
}
