using Speckle.GSA.API.GwaSchema;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  [GsaType(GwaKeyword.GRID_PLANE, GwaSetCommandType.Set, true, true, true, GwaKeyword.AXIS)]
  
  public class GsaGridPlaneParser: GwaParser<GsaGridPlane>
  {
    public GsaGridPlaneParser(GsaGridPlane gsaGridPlane) : base(gsaGridPlane) { }
    public GsaGridPlaneParser() : base(new GsaGridPlane()) { }

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
      AddItems(ref items, record.Name, AddType(), AddAxis(), record.Elevation ?? 0, 
        record.StoreyToleranceBelowAuto ? 0 : record.StoreyToleranceBelow, 
        record.StoreyToleranceAboveAuto ? 0 : record.StoreyToleranceAbove);

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private string AddType()
    {
      return (record.Type == GridPlaneType.Storey) ? "STOREY" : "GENERAL";
    }

    private string AddAxis()
    {
      if (record.AxisRefType == GridPlaneAxisRefType.Reference)
      {
        return (record.AxisIndex ?? 0).ToString();
      }
      switch (record.AxisRefType)
      {
        case GridPlaneAxisRefType.XElevation: return (-11).ToString();
        case GridPlaneAxisRefType.YElevation: return (-12).ToString();
        case GridPlaneAxisRefType.GlobalCylindrical: return (-13).ToString();
        default: return 0.ToString();
      }
    }
    #endregion

    #region from_gwa_fns
    protected bool AddName(string v)
    {
      record.Name = (string.IsNullOrEmpty(v)) ? null : v;
      return true;
    }

    private bool AddType(string v)
    {
      record.Type = v.ToUpperInvariant().Equals("STOREY") ? GridPlaneType.Storey : GridPlaneType.General;
      return true;
    }

    private bool AddAxis(string v)
    {
      record.AxisIndex = null;
      if (int.TryParse(v, out var intVal))
      {
        if (intVal > 0)
        {
          record.AxisRefType = GridPlaneAxisRefType.Reference;
          record.AxisIndex = intVal;
        }
        else
        {
          switch (intVal)
          {
            case -11: record.AxisRefType = GridPlaneAxisRefType.XElevation; break;
            case -12: record.AxisRefType = GridPlaneAxisRefType.YElevation; break;
            case -13: record.AxisRefType = GridPlaneAxisRefType.GlobalCylindrical; break;
            default: record.AxisRefType = GridPlaneAxisRefType.Global; break;
          }
        }
      }
      else
      {
        record.AxisRefType = GridPlaneAxisRefType.Global;
      }
      return true;
    }

    private bool AddElev(string v)
    {
      record.Elevation = (double.TryParse(v, out var elev)) ? (double?)elev : null;
      return true;
    }

    private bool AddBelow(string v)
    {
      if (double.TryParse(v, out var tol))
      {
        record.StoreyToleranceBelowAuto = (tol == 0);
        record.StoreyToleranceBelow = (record.StoreyToleranceBelowAuto) ? null : (double?)tol;
      }
      else
      {
        record.StoreyToleranceBelowAuto = true;
        record.StoreyToleranceBelow = null;
      }
      return true;
    }

    private bool AddAbove(string v)
    {
      if (double.TryParse(v, out var tol))
      {
        record.StoreyToleranceAboveAuto = (tol == 0);
        record.StoreyToleranceAbove = (record.StoreyToleranceAboveAuto) ? null : (double?)tol;
      }
      else
      {
        record.StoreyToleranceAboveAuto = true;
        record.StoreyToleranceAbove = null;
      }
      return true;
    }
    #endregion
  }
}
