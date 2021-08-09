using System;
using System.Collections.Generic;
using System.Linq;


namespace Speckle.GSA.API.GwaSchema
{
  [GsaType(GwaKeyword.PROP_2D, GwaSetCommandType.Set, true, GwaKeyword.MAT_STEEL, GwaKeyword.MAT_CONCRETE, GwaKeyword.AXIS)]
  public class GsaProp2d : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public Colour Colour = Colour.NO_RGB;
    public Property2dType Type;
    public AxisRefType AxisRefType;
    public int? AxisIndex;
    public int? AnalysisMaterialIndex;
    public Property2dMaterialType MatType;
    public int? GradeIndex;
    public int? DesignIndex;
    public double? Thickness;
    public string Profile = ""; //Not supported yet
    public Property2dRefSurface RefPt;
    public double RefZ;
    public double Mass;
    //For each of these next 4 pairs, only one will be filled per pair, depending on the presence or absense of the % sign
    public double? BendingStiffnessPercentage;
    public double? Bending;
    public double? ShearStiffnessPercentage;
    public double? Shear;
    public double? InPlaneStiffnessPercentage;
    public double? InPlane;
    public double? VolumePercentage;
    public double? Volume;

    public GsaProp2d() : base()
    {
      //Defaults
      Version = 7;
    }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }
      var items = remainingItems;
      FromGwaByFuncs(items, out remainingItems, AddName);

      //PROP_2D.7 | num | name | colour | type | axis | mat | mat_type | grade | design | profile | ref_pt | ref_z | mass | flex | shear | inplane | weight |
      if (!FromGwaByFuncs(items, out remainingItems, AddName, (v) => AddColour(v, out Colour), (v) => Enum.TryParse(v, true, out Type), 
        AddAxis, (v) => AddNullableIndex(v, out AnalysisMaterialIndex), 
        (v) => Enum.TryParse(v, true, out MatType), (v) => AddNullableIndex(v, out GradeIndex), (v) => AddNullableIndex(v, out DesignIndex), 
        AddThickness, (v) => v.TryParseStringValue(out RefPt), (v) => double.TryParse(v, out RefZ),
        (v) => double.TryParse(v, out Mass),
        (v) => AddPercentageOrValue(v, out BendingStiffnessPercentage, out Bending),
        (v) => AddPercentageOrValue(v, out ShearStiffnessPercentage, out Shear),
        (v) => AddPercentageOrValue(v, out InPlaneStiffnessPercentage, out InPlane),
        (v) => AddPercentageOrValue(v, out VolumePercentage, out Volume)))
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

      //PROP_2D.7 | num | name | colour | type | axis | mat | mat_type | grade | design | profile | ref_pt | ref_z | mass | flex | shear | inplane | weight |
      AddItems(ref items, Name, "NO_RGB", Type.ToString().ToUpper(), AddAxis(), AnalysisMaterialIndex ?? 0, MatType.ToString().ToUpper(),
        GradeIndex ?? 0, DesignIndex ?? 0, Thickness ?? 0, RefPt.GetStringValue(), RefZ, Mass,
        AddPercentageOrValue(BendingStiffnessPercentage, Bending), AddPercentageOrValue(ShearStiffnessPercentage, Shear),
        AddPercentageOrValue(InPlaneStiffnessPercentage, InPlane), AddPercentageOrValue(VolumePercentage, Volume));

      gwa = (Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return gwa.Count() > 0;
    }

    #region to_gwa_fns
    private string AddPercentageOrValue(double? percentage, double? value)
    {
      if (percentage.HasValue)
      {
        return percentage.Value + "%";
      }
      return value.Value.ToString();
    }

    private string AddAxis()
    {
      if (AxisRefType == AxisRefType.Reference)
      {
        return AxisIndex.ToString();
      }
      else if (AxisRefType == AxisRefType.NotSet)
      {
        return AxisRefType.Global.ToString().ToUpperInvariant();
      }
      return AxisRefType.ToString().ToUpperInvariant();
    }
    #endregion

    #region from_gwa_fns
    private bool AddThickness(string v)
    {
      if (double.TryParse(v, out double thickness))
      {
        this.Thickness = thickness;
      }
      return true;
    }

    private bool AddPercentageOrValue(string v, out double? percentage, out double? value)
    {
      percentage = null;
      value = null;
      if (v.EndsWith("%") && double.TryParse(v.Split('%').First(), out double p))
      {
        percentage = p;
        return true;
      }
      else if (double.TryParse(v, out double val))
      {
        value = val;
      }
      return false;
    }

    private bool AddAxis(string v)
    {
      if (v.Trim().Equals(AxisRefType.Global.ToString(), StringComparison.InvariantCultureIgnoreCase))
      {
        AxisRefType = AxisRefType.Global;
        return true;
      }
      if (v.Trim().Equals(AxisRefType.Local.ToString(), StringComparison.InvariantCultureIgnoreCase))
      {
        AxisRefType = AxisRefType.Local;
        return true;
      }
      else
      {
        AxisRefType = AxisRefType.Reference;
        return AddNullableIndex(v, out AxisIndex);
      }
    }
    #endregion
  }
}
