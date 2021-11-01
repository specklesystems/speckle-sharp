using Speckle.GSA.API;
using System.Collections.Generic;
using System.Linq;
using Speckle.GSA.API.GwaSchema;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  //polygon references not supported yet
  [GsaType(GwaKeyword.GRID_SURFACE, GwaSetCommandType.Set, true, true, true, new[] { GwaKeyword.MEMB, GwaKeyword.EL, GwaKeyword.GRID_PLANE })]
  
  public class GsaGridSurfaceParser : GwaParser<GsaGridSurface>
  {
    public GsaGridSurfaceParser(GsaGridSurface gsaGridSurface) : base(gsaGridSurface) { }

    public GsaGridSurfaceParser() : base(new GsaGridSurface()) { }

    private static double multiplePerAngleDegree = 57.2957795;

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }

      //GRID_SURFACE.1 | num | name | plane | type | elements | tol | span | angle | grid
      return FromGwaByFuncs(remainingItems, out var _, AddName, AddPlane, AddType, (v) => AddEntities(v, out record.MemberIndices, out record.ElementIndices), AddTol, AddSpan, AddAngle, AddGrid);
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //GRID_SURFACE.1 | num | name | plane | type | elements | tol | span | angle | grid
      AddItems(ref items, record.Name,
        AddPlane(),
        ((record.Type == GridSurfaceElementsType.OneD) ? 1 : 2).ToString(),
        AddEntities(record.MemberIndices, record.ElementIndices),
        record.Tolerance ?? 0, AddSpan(),
        AddAngle(),
        record.Expansion.GridExpansionToString());

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private string AddPlane()
    {
      if (record.PlaneRefType == GridPlaneAxisRefType.Reference)
      {
        return (record.PlaneIndex ?? 0).ToString();
      }
      switch (record.PlaneRefType)
      {
        case GridPlaneAxisRefType.XElevation: return (-11).ToString();
        case GridPlaneAxisRefType.YElevation: return (-12).ToString();
        case GridPlaneAxisRefType.GlobalCylindrical: return (-13).ToString();
        default: return 0.ToString();  //This is for global
      }
    }

    private string AddAngle()
    {
      return ((record.Angle ?? 0) * multiplePerAngleDegree).ToString();
    }

    private string AddSpan()
    {
      return (record.Span == GridSurfaceSpan.One) ? "ONE" : "TWO_SIMPLE";
    }
    #endregion

    #region from_gwa_fns
    protected bool AddName(string v)
    {
      record.Name = (string.IsNullOrEmpty(v)) ? null : v;
      return true;
    }

    private bool AddPlane(string v)
    {
      record.PlaneIndex = null;
      if (int.TryParse(v, out var intVal))
      {
        if (intVal > 0)
        {
          record.PlaneRefType = GridPlaneAxisRefType.Reference;
          record.PlaneIndex = intVal;
        }
        else
        {
          switch (intVal)
          {
            case -11: record.PlaneRefType = GridPlaneAxisRefType.XElevation; break;
            case -12: record.PlaneRefType = GridPlaneAxisRefType.YElevation; break;
            case -13: record.PlaneRefType = GridPlaneAxisRefType.GlobalCylindrical; break;
            default: record.PlaneRefType = GridPlaneAxisRefType.Global; break;
          }
        }
      }
      else
      {
        record.PlaneRefType = GridPlaneAxisRefType.Global;
      }
      return true;
    }

    private bool AddType(string v)
    {
      record.Type = (!string.IsNullOrEmpty(v) && int.TryParse(v, out var intVal) && intVal >= 0 && intVal <= 2)
        ? (GridSurfaceElementsType)intVal
        : GridSurfaceElementsType.NotSet;
      return true;
    }

    private bool AddSpan(string v)
    {
      record.Span = (!string.IsNullOrEmpty(v) && v.ToUpperInvariant().StartsWith("ONE")) ? GridSurfaceSpan.One : GridSurfaceSpan.Two;
      return true;
    }

    private bool AddTol(string v)
    {
      record.Tolerance = (double.TryParse(v, out var tol) && tol > 0) ? (double?)tol : null;
      return true;
    }

    private bool AddAngle(string v)
    {
      var gwaAngle = (double.TryParse(v, out var angle)) ? angle : 0;
      record.Angle = gwaAngle / multiplePerAngleDegree;
      return true;
    }

    private bool AddGrid(string v)
    {
      record.Expansion = v.StringToGridExpansion();
      return true;
    }
    #endregion
  }
}
