using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  //polygon references not supported yet
  [GsaType(GwaKeyword.LOAD_GRID_POINT, GwaSetCommandType.SetAt, true, GwaKeyword.GRID_SURFACE, GwaKeyword.LOAD_TITLE, GwaKeyword.AXIS)]
  public class GsaLoadGridPointParser : GwaParser<GsaLoadGridPoint>
  {
    public GsaLoadGridPointParser(GsaLoadGridPoint gsaLoadGridPoint) : base(gsaLoadGridPoint) { }

    public GsaLoadGridPointParser() : base(new GsaLoadGridPoint()) { }

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
      AddItems(ref items, record.Name, record.GridSurfaceIndex, record.X, record.Y, record.LoadCaseIndex ?? 0, AddAxis(), record.LoadDirection, record.Value);

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
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

    private bool AddX(string v)
    {
      record.X = (double.TryParse(v, out var x) && x != 0) ? (double?)x : null;
      return true;
    }

    private bool AddY(string v)
    {
      record.Y = (double.TryParse(v, out var y) && y != 0) ? (double?)y : null;
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
