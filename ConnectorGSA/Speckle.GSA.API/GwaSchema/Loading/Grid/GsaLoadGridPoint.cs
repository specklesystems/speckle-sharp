using System;
using System.Collections.Generic;
using System.Linq;


namespace Speckle.GSA.API.GwaSchema
{
  //polygon references not supported yet
  [GsaType(GwaKeyword.LOAD_GRID_POINT, GwaSetCommandType.SetAt, true, GwaKeyword.GRID_SURFACE, GwaKeyword.LOAD_TITLE, GwaKeyword.AXIS)]
  public class GsaLoadGridPoint : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public int? GridSurfaceIndex;
    public double? X;
    public double? Y;
    public int? LoadCaseIndex;
    public AxisRefType AxisRefType;
    public int? AxisIndex;
    public AxisDirection3 LoadDirection;
    public double? Value;

    public GsaLoadGridPoint() : base()
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

      //LOAD_GRID_POINT.2 | name | grid_surface | x | y | case | axis | dir | value
      return FromGwaByFuncs(remainingItems, out var _, AddName, AddSurface, AddX, AddY, AddCase, AddAxis, AddDir, AddValue);
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //LOAD_GRID_POINT.2 | name | grid_surface | x | y | case | axis | dir | value
      AddItems(ref items, Name, GridSurfaceIndex, X, Y, LoadCaseIndex ?? 0, AddAxis(), LoadDirection, Value);

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private string AddAxis()
    {
      if (AxisRefType == AxisRefType.Reference && AxisIndex.HasValue)
      {
        return AxisIndex.Value.ToString();
      }
      if (AxisRefType == AxisRefType.Local)
      {
        return "LOCAL";
      }
      //Assume global to be the default
      return "GLOBAL";
    }

    private string AddLoadDirection()
    {
      return (LoadDirection == AxisDirection3.NotSet) ? "X" : LoadDirection.ToString();
    }
    #endregion

    #region from_gwa_fns
    private bool AddSurface(string v)
    {
      GridSurfaceIndex = (int.TryParse(v, out var surfaceIndex) && surfaceIndex > 0) ? (int?)surfaceIndex : null;
      return true;
    }

    private bool AddX(string v)
    {
      X = (double.TryParse(v, out var x) && x != 0) ? (double?)x : null;
      return true;
    }

    private bool AddY(string v)
    {
      Y = (double.TryParse(v, out var y) && y != 0) ? (double?)y : null;
      return true;
    }

    private bool AddCase(string v)
    {
      LoadCaseIndex = (int.TryParse(v, out var loadCaseIndex) && loadCaseIndex > 0) ? (int?)loadCaseIndex : null;
      return true;
    }

    private bool AddAxis(string v)
    {
      AxisRefType = Enum.TryParse<AxisRefType>(v, true, out var refType) ? refType : AxisRefType.NotSet;
      if (AxisRefType == AxisRefType.NotSet && int.TryParse(v, out var axisIndex) && axisIndex > 0)
      {
        AxisRefType = AxisRefType.Reference;
        AxisIndex = axisIndex;
      }
      return true;
    }

    private bool AddDir(string v)
    {
      LoadDirection = Enum.TryParse<AxisDirection3>(v, true, out var loadDir) ? loadDir : AxisDirection3.NotSet;
      return true;
    }

    private bool AddValue(string v)
    {
      Value = (double.TryParse(v, out var value) && value != 0) ? (double?)value : null;
      return true;
    }
    #endregion
  }
}
