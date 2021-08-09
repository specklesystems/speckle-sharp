using System;
using System.Collections.Generic;
using System.Linq;


namespace Speckle.GSA.API.GwaSchema
{
  //polygon references not supported yet
  [GsaType(GwaKeyword.LOAD_GRID_LINE, GwaSetCommandType.SetAt, true, GwaKeyword.GRID_SURFACE, GwaKeyword.LOAD_TITLE, GwaKeyword.AXIS)]
  public class GsaLoadGridLine : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public int? GridSurfaceIndex;
    public LoadLineOption Line;
    public int? PolygonIndex;
    public string Polygon;
    public int? LoadCaseIndex;
    public AxisRefType AxisRefType;
    public int? AxisIndex;
    public bool Projected;
    public AxisDirection3 LoadDirection;
    public double? Value1;
    public double? Value2;

    public GsaLoadGridLine() : base()
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

      //LOAD_GRID_LINE.2 | name | grid_surface | line | poly | case | axis | proj | dir | value_1 | value_2
      return FromGwaByFuncs(remainingItems, out var _, AddName, AddSurface, AddLine, AddPoly, AddCase, AddAxis, AddProj, AddDir, AddValue1, AddValue2);
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //LOAD_GRID_LINE.2 | name | grid_surface | line | poly | case | axis | proj | dir | value_1 | value_2
      AddItems(ref items, Name, GridSurfaceIndex, Line.ToString().ToUpper(), AddPoly(), LoadCaseIndex ?? 0, AddAxis(), AddProj(), LoadDirection.ToString(), Value1, Value2);

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private string AddPoly()
    {
      if (Line == LoadLineOption.PolyRef)
      {
        return PolygonIndex.Value.ToString();
      }
      if (Line == LoadLineOption.Polygon)
      {
        return Polygon;
      }
      return "";
    }

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

    private string AddProj()
    {
      return (Projected) ? "YES" : "NO";
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

    private bool AddLine(string v)
    {
      if (Enum.TryParse<LoadLineOption>(v, true, out var loadLineOption))
      {
        Line = loadLineOption;
      }
      else
      {
        Line = LoadLineOption.NotSet;
      }
      return true;
    }

    private bool AddPoly(string v)
    {
      Polygon = (Line == LoadLineOption.Polygon) ? v : null;
      PolygonIndex = (Line == LoadLineOption.PolyRef && int.TryParse(v, out var index) && index > 0) ? (int?)index : null;
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

    private bool AddProj(string v)
    {
      Projected = (v.Equals("yes", StringComparison.InvariantCultureIgnoreCase));
      return true;
    }

    private bool AddDir(string v)
    {
      LoadDirection = Enum.TryParse<AxisDirection3>(v, true, out var loadDir) ? loadDir : AxisDirection3.NotSet;
      return true;
    }

    private bool AddValue1(string v)
    {
      Value1 = (double.TryParse(v, out var value) && value != 0) ? (double?)value : null;
      return true;
    }

    private bool AddValue2(string v)
    {
      Value2 = (double.TryParse(v, out var value) && value != 0) ? (double?)value : null;
      return true;
    }
    #endregion
  }
}
