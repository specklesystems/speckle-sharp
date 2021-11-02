using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  [GsaType(GwaKeyword.PROP_2D, GwaSetCommandType.Set, true, GwaKeyword.MAT_STEEL, GwaKeyword.MAT_CONCRETE, GwaKeyword.AXIS)]
  public class GsaProp2dParser : GwaParser<GsaProp2d>
  {
    public GsaProp2dParser(GsaProp2d gsaProp2D) : base(gsaProp2D) { }

    public GsaProp2dParser() : base(new GsaProp2d()) { }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }
      var items = remainingItems;
      FromGwaByFuncs(items, out remainingItems, AddName);

      //PROP_2D.7 | num | name | colour | type | axis | mat | mat_type | grade | design | profile | ref_pt | ref_z | mass | flex | shear | inplane | weight |
      if (!FromGwaByFuncs(items, out remainingItems, AddName, (v) => AddColour(v, out record.Colour), (v) => Enum.TryParse(v, true, out record.Type), 
        AddAxis, (v) => AddNullableIndex(v, out record.AnalysisMaterialIndex), 
        (v) => Enum.TryParse(v, true, out record.MatType), (v) => AddNullableIndex(v, out record.GradeIndex), (v) => AddNullableIndex(v, out record.DesignIndex), 
        AddThickness, (v) => v.TryParseStringValue(out record.RefPt), (v) => double.TryParse(v, out record.RefZ),
        (v) => double.TryParse(v, out record.Mass),
        (v) => AddPercentageOrValue(v, out record.BendingStiffnessPercentage, out record.Bending),
        (v) => AddPercentageOrValue(v, out record.ShearStiffnessPercentage, out record.Shear),
        (v) => AddPercentageOrValue(v, out record.InPlaneStiffnessPercentage, out record.InPlane),
        (v) => AddPercentageOrValue(v, out record.VolumePercentage, out record.Volume)))
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
      AddItems(ref items, record.Name, "NO_RGB", record.Type.ToString().ToUpper(), AddAxis(), record.AnalysisMaterialIndex ?? 0, record.MatType.ToString().ToUpper(),
        record.GradeIndex ?? 0, record.DesignIndex ?? 0, AddThickness(), record.RefPt.GetStringValue(), record.RefZ, record.Mass,
        AddPercentageOrValue(record.BendingStiffnessPercentage, record.Bending), AddPercentageOrValue(record.ShearStiffnessPercentage, record.Shear),
        AddPercentageOrValue(record.InPlaneStiffnessPercentage, record.InPlane), AddPercentageOrValue(record.VolumePercentage, record.Volume));

      gwa = (Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return gwa.Count() > 0;
    }

    #region to_gwa_fns
    private string AddThickness()
    {
      //TO DO - use a new thickness member in the schema class which stores units for this
      return (record.Thickness ?? 0) + "(mm)";
    }

    private string AddPercentageOrValue(double? percentage, double? value)
    {
      if(percentage == null && value == null)
      {
        return "100%"; 
      } else
      {
        if (percentage.HasValue)
        {
          return percentage.Value + "%";
        }
        return value.Value.ToString();
      }
    }

    private string AddAxis()
    {
      if (record.AxisRefType == AxisRefType.Reference)
      {
        return record.AxisIndex.ToString();
      }
      else if (record.AxisRefType == AxisRefType.NotSet)
      {
        return AxisRefType.Global.ToString().ToUpperInvariant();
      }
      return record.AxisRefType.ToString().ToUpperInvariant();
    }
    #endregion

    #region from_gwa_fns
    protected bool AddName(string v)
    {
      record.Name = (string.IsNullOrEmpty(v)) ? null : v;
      return true;
    }

    private bool AddThickness(string v)
    {
      var vt = v.Contains('(') ? v.Split('(')[0] : v;
      if (double.TryParse(vt, out double thickness))
      {
        record.Thickness = thickness;
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
        record.AxisRefType = AxisRefType.Global;
        return true;
      }
      if (v.Trim().Equals(AxisRefType.Local.ToString(), StringComparison.InvariantCultureIgnoreCase))
      {
        record.AxisRefType = AxisRefType.Local;
        return true;
      }
      else
      {
        record.AxisRefType = AxisRefType.Reference;
        return AddNullableIndex(v, out record.AxisIndex);
      }
    }
    #endregion
  }
}
