using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  //polygon references not supported yet
  [GsaType(GwaKeyword.LOAD_GRID_AREA, GwaSetCommandType.SetAt, true, true, true, GwaKeyword.GRID_SURFACE, GwaKeyword.AXIS, GwaKeyword.LOAD_TITLE)]
  public class GsaLoadGridAreaParser : GwaParser<GsaLoadGridArea>
  {
    public GsaLoadGridAreaParser(GsaLoadGridArea gsaLoadGridArea) : base(gsaLoadGridArea) { }

    public GsaLoadGridAreaParser() : base(new GsaLoadGridArea()) { }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }

      //LOAD_GRID_AREA.2 | name | grid_surface | area | poly | case | axis | proj | dir | value
      return FromGwaByFuncs(remainingItems, out var _, AddName, AddSurface, AddArea, AddPoly, AddCase, AddAxis, AddProj, AddDir, AddValue);
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if(!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //LOAD_GRID_AREA.2 | name | grid_surface | area | poly | case | axis | proj | dir | value
      AddItems(ref items, record.Name, record.GridSurfaceIndex, record.Area.ToString().ToUpper(), AddPoly(), record.LoadCaseIndex ?? 0, AddAxis(), AddProj(), record.LoadDirection, record.Value);

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private string AddPoly()
    {
      if (record.Area == LoadAreaOption.PolyRef)
      {
        return record.PolygonIndex.Value.ToString();
      }
      if (record.Area == LoadAreaOption.Polygon)
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

    private bool AddArea(string v)
    {
      if (Enum.TryParse<LoadAreaOption>(v, true, out var loadAreaOption))
      {
        record.Area = loadAreaOption;
      }
      else
      {
        record.Area = LoadAreaOption.NotSet;
      }
      return true;
    }

    private bool AddPoly(string v)
    {
      record.Polygon = (record.Area == LoadAreaOption.Polygon) ? v : null;
      record.PolygonIndex = (record.Area == LoadAreaOption.PolyRef && int.TryParse(v, out var index) && index > 0) ? (int?) index : null;
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

    private bool AddValue(string v)
    {
      record.Value = (double.TryParse(v, out var value) && value != 0) ? (double?)value : null;
      return true;
    }
    #endregion
  }
}
