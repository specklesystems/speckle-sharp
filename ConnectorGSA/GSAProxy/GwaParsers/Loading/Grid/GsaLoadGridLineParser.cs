using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  //polygon references not supported yet
  [GsaType(GwaKeyword.LOAD_GRID_LINE, GwaSetCommandType.SetAt, true, GwaKeyword.GRID_SURFACE, GwaKeyword.LOAD_TITLE, GwaKeyword.AXIS)]
  public class GsaLoadGridLineParser : GwaParser<GsaLoadGridLine>
  {
    public GsaLoadGridLineParser(GsaLoadGridLine gsaLoadGridLine) : base(gsaLoadGridLine) { }

    public GsaLoadGridLineParser() : base(new GsaLoadGridLine()) { }

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
      AddItems(ref items, record.Name, record.GridSurfaceIndex, record.Line.ToString().ToUpper(), AddPoly(), record.LoadCaseIndex ?? 0, 
        AddAxis(), AddProj(), record.LoadDirection.ToString(), record.Value1, record.Value2);

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private string AddPoly()
    {
      if (record.Line == LoadLineOption.PolyRef)
      {
        return record.PolygonIndex.Value.ToString();
      }
      if (record.Line == LoadLineOption.Polygon)
      {
        return record.Polygon;
      }
      return "";
    }

    private string AddAxis()
    {
      if (record.AxisRefType == AxisRefType.Reference && record.AxisIndex.HasValue)
      {
        return record.AxisIndex.Value.ToString();
      }
      if (record.AxisRefType == AxisRefType.Local)
      {
        return "LOCAL";
      }
      //Assume global to be the default
      return "GLOBAL";
    }

    private string AddProj()
    {
      return (record.Projected) ? "YES" : "NO";
    }

    private string AddLoadDirection()
    {
      return (record.LoadDirection == AxisDirection3.NotSet) ? "X" : record.LoadDirection.ToString();
    }
    #endregion

    #region from_gwa_fns
    protected bool AddName(string v)
    {
      record.Name = (string.IsNullOrEmpty(v)) ? null : v;
      return true;
    }

    private bool AddSurface(string v)
    {
      record.GridSurfaceIndex = (int.TryParse(v, out var surfaceIndex) && surfaceIndex > 0) ? (int?)surfaceIndex : null;
      return true;
    }

    private bool AddLine(string v)
    {
      if (Enum.TryParse<LoadLineOption>(v, true, out var loadLineOption))
      {
        record.Line = loadLineOption;
      }
      else
      {
        record.Line = LoadLineOption.NotSet;
      }
      return true;
    }

    private bool AddPoly(string v)
    {
      record.Polygon = (record.Line == LoadLineOption.Polygon) ? v : null;
      record.PolygonIndex = (record.Line == LoadLineOption.PolyRef && int.TryParse(v, out var index) && index > 0) ? (int?)index : null;
      return true;
    }

    private bool AddCase(string v)
    {
      record.LoadCaseIndex = (int.TryParse(v, out var loadCaseIndex) && loadCaseIndex > 0) ? (int?)loadCaseIndex : null;
      return true;
    }

    private bool AddAxis(string v)
    {
      record.AxisRefType = Enum.TryParse<AxisRefType>(v, true, out var refType) ? refType : AxisRefType.NotSet;
      if (record.AxisRefType == AxisRefType.NotSet && int.TryParse(v, out var axisIndex) && axisIndex > 0)
      {
        record.AxisRefType = AxisRefType.Reference;
        record.AxisIndex = axisIndex;
      }
      return true;
    }

    private bool AddProj(string v)
    {
      record.Projected = (v.Equals("yes", StringComparison.InvariantCultureIgnoreCase));
      return true;
    }

    private bool AddDir(string v)
    {
      record.LoadDirection = Enum.TryParse<AxisDirection3>(v, true, out var loadDir) ? loadDir : AxisDirection3.NotSet;
      return true;
    }

    private bool AddValue1(string v)
    {
      record.Value1 = (double.TryParse(v, out var value) && value != 0) ? (double?)value : null;
      return true;
    }

    private bool AddValue2(string v)
    {
      record.Value2 = (double.TryParse(v, out var value) && value != 0) ? (double?)value : null;
      return true;
    }
    #endregion
  }
}
