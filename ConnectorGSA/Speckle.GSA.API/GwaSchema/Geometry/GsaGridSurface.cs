using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


namespace Speckle.GSA.API.GwaSchema
{
  //polygon references not supported yet
  [GsaType(GwaKeyword.GRID_SURFACE, GwaSetCommandType.Set, true, true, true, new[] { GwaKeyword.MEMB, GwaKeyword.EL, GwaKeyword.GRID_PLANE })]
  public class GsaGridSurface : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public GridPlaneAxisRefType PlaneRefType;
    public int? PlaneIndex;
    public GridSurfaceElementsType Type;  
    //public bool AllIndices = false;
    public List<int> Entities = new List<int>();
    public double? Tolerance;
    public GridSurfaceSpan Span;
    public double? Angle;  //Degrees
    public GridExpansion Expansion;

    private static double multiplePerAngleDegree = 57.2957795;

    public GsaGridSurface() : base()
    {
      //Defaults
      Version = 1;
    }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }

      //GRID_SURFACE.1 | num | name | plane | type | elements | tol | span | angle | grid
      return FromGwaByFuncs(remainingItems, out var _, AddName, AddPlane, AddType, (v) => AddEntities(v, out Entities), AddTol, AddSpan, AddAngle, AddGrid);
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //GRID_SURFACE.1 | num | name | plane | type | elements | tol | span | angle | grid
      AddItems(ref items, Name, 
        AddPlane(), 
        ((Type == GridSurfaceElementsType.OneD) ? 1 : 2).ToString(), 
        AddEntities(Entities), 
        Tolerance ?? 0, AddSpan(),
        AddAngle(),
        Expansion.GridExpansionToString());

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private string AddPlane()
    {
      if (PlaneRefType == GridPlaneAxisRefType.Reference)
      {
        return (PlaneIndex ?? 0).ToString();
      }
      switch (PlaneRefType)
      {
        case GridPlaneAxisRefType.XElevation: return (-11).ToString();
        case GridPlaneAxisRefType.YElevation: return (-12).ToString();
        case GridPlaneAxisRefType.GlobalCylindrical: return (-13).ToString();
        default: return 0.ToString();  //This is for global
      }
    }

    private string AddAngle()
    {
      return ((Angle ?? 0) * multiplePerAngleDegree).ToString();
    }

    private string AddSpan()
    {
      return (Span == GridSurfaceSpan.One) ? "ONE" : "TWO_SIMPLE";
    }
    #endregion

    #region from_gwa_fns
    private bool AddPlane(string v)
    {
      PlaneIndex = null;
      if (int.TryParse(v, out var intVal))
      {
        if (intVal > 0)
        {
          PlaneRefType = GridPlaneAxisRefType.Reference;
          PlaneIndex = intVal;
        }
        else
        {
          switch (intVal)
          {
            case -11: PlaneRefType = GridPlaneAxisRefType.XElevation; break;
            case -12: PlaneRefType = GridPlaneAxisRefType.YElevation; break;
            case -13: PlaneRefType = GridPlaneAxisRefType.GlobalCylindrical; break;
            default: PlaneRefType = GridPlaneAxisRefType.Global; break;
          }
        }
      }
      else
      {
        PlaneRefType = GridPlaneAxisRefType.Global;
      }
      return true;
    }

    private bool AddType(string v)
    {
      Type = (!string.IsNullOrEmpty(v) && int.TryParse(v, out var intVal) && intVal >= 0 && intVal <= 2)
        ? (GridSurfaceElementsType)intVal
        : GridSurfaceElementsType.NotSet;
      return true;
    }

    private bool AddSpan(string v)
    {
      Span = (!string.IsNullOrEmpty(v) && v.ToUpperInvariant().StartsWith("ONE")) ? GridSurfaceSpan.One : GridSurfaceSpan.Two;
      return true;
    }

    private bool AddTol(string v)
    {
      Tolerance = (double.TryParse(v, out var tol) && tol > 0) ? (double?)tol : null;
      return true;
    }

    private bool AddAngle(string v)
    {
      var gwaAngle = (double.TryParse(v, out var angle)) ? angle : 0;
      Angle = gwaAngle / multiplePerAngleDegree;
      return true;
    }

    private bool AddGrid(string v)
    {
      Expansion = v.StringToGridExpansion();
      return true;
    }
    #endregion
  }
}
