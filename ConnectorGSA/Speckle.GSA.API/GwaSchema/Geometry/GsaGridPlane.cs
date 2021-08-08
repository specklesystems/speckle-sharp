using System.Collections.Generic;
using System.Linq;


namespace Speckle.GSA.API.GwaSchema
{
  [GsaType(GwaKeyword.GRID_PLANE, GwaSetCommandType.Set, true, true, true, GwaKeyword.AXIS)]
  public class GsaGridPlane : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public GridPlaneType Type;
    public GridPlaneAxisRefType AxisRefType;
    public int? AxisIndex;
    public double? Elevation;
    public bool StoreyToleranceBelowAuto;
    public double? StoreyToleranceBelow;
    public bool StoreyToleranceAboveAuto;
    public double? StoreyToleranceAbove;

    public GsaGridPlane() : base()
    {
      //Defaults
      Version = 4;
    }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }
      //Constructed from using 10.1 rather than consulting docs at https://www.oasys-software.com/help/gsa/10.1/GSA_Text.html
      //which, at the time of writing, only reports up to version 3
      //GRID_PLANE.4 | num | name | type | axis | elev | below | above
      return FromGwaByFuncs(remainingItems, out var _, AddName, AddType, AddAxis, AddElev, AddBelow, AddAbove);
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //GRID_PLANE.4 | num | name | type | axis | elev | below | above
      AddItems(ref items, Name, AddType(), AddAxis(), Elevation ?? 0, StoreyToleranceBelowAuto ? 0 : StoreyToleranceBelow, StoreyToleranceAboveAuto ? 0 : StoreyToleranceAbove);

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private string AddType()
    {
      return (Type == GridPlaneType.Storey) ? "STOREY" : "GENERAL";
    }

    private string AddAxis()
    {
      if (AxisRefType == GridPlaneAxisRefType.Reference)
      {
        return (AxisIndex ?? 0).ToString();
      }
      switch (AxisRefType)
      {
        case GridPlaneAxisRefType.XElevation: return (-11).ToString();
        case GridPlaneAxisRefType.YElevation: return (-12).ToString();
        case GridPlaneAxisRefType.GlobalCylindrical: return (-13).ToString();
        default: return 0.ToString();
      }
    }
    #endregion

    #region from_gwa_fns
    private bool AddType(string v)
    {
      Type = v.ToUpperInvariant().Equals("STOREY") ? GridPlaneType.Storey : GridPlaneType.General;
      return true;
    }

    private bool AddAxis(string v)
    {
      AxisIndex = null;
      if (int.TryParse(v, out var intVal))
      {
        if (intVal > 0)
        {
          AxisRefType = GridPlaneAxisRefType.Reference;
          AxisIndex = intVal;
        }
        else
        {
          switch (intVal)
          {
            case -11: AxisRefType = GridPlaneAxisRefType.XElevation; break;
            case -12: AxisRefType = GridPlaneAxisRefType.YElevation; break;
            case -13: AxisRefType = GridPlaneAxisRefType.GlobalCylindrical; break;
            default: AxisRefType = GridPlaneAxisRefType.Global; break;
          }
        }
      }
      else
      {
        AxisRefType = GridPlaneAxisRefType.Global;
      }
      return true;
    }

    private bool AddElev(string v)
    {
      Elevation = (double.TryParse(v, out var elev)) ? (double?)elev : null;
      return true;
    }

    private bool AddBelow(string v)
    {
      if (double.TryParse(v, out var tol))
      {
        StoreyToleranceBelowAuto = (tol == 0);
        StoreyToleranceBelow = (StoreyToleranceBelowAuto) ? null : (double?)tol;
      }
      else
      {
        StoreyToleranceBelowAuto = true;
        StoreyToleranceBelow = null;
      }
      return true;
    }

    private bool AddAbove(string v)
    {
      if (double.TryParse(v, out var tol))
      {
        StoreyToleranceAboveAuto = (tol == 0);
        StoreyToleranceAbove = (StoreyToleranceAboveAuto) ? null : (double?)tol;
      }
      else
      {
        StoreyToleranceAboveAuto = true;
        StoreyToleranceAbove = null;
      }
      return true;
    }
    #endregion
  }
}
