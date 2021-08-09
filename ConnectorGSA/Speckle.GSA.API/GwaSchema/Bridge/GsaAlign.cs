using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;


namespace Speckle.GSA.API.GwaSchema.Bridge
{
  [GsaType(GwaKeyword.ALIGN, GwaSetCommandType.Set, true, GwaKeyword.GRID_SURFACE)]
  public class GsaAlign : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public int? GridSurfaceIndex;
    public int? NumAlignmentPoints;
    public List<double> Chain;
    public List<double> Curv;

    public GsaAlign() : base()
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

      var items = remainingItems;
      //ALIGN | num | name | surface | num ( | chain | curv )
      if (!FromGwaByFuncs(items, out remainingItems, AddName, AddSurface, AddNumAlignmentPoints))
      {
        return false;
      }

      //Loop through remaining items and add pairs of points to Chain and Curv lists
      Chain = new List<double>();
      Curv = new List<double>();
      for (var i = 0; i < NumAlignmentPoints; i++)
      {
        items = remainingItems;
        if (!FromGwaByFuncs(items, out remainingItems, AddChain, AddCurv))
        {
          return false;
        }
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

      //ALIGN | num | name | surface | num ( | chain | curv )
      AddItems(ref items, Name, GridSurfaceIndex, NumAlignmentPoints);

      for (var i = 0; i < NumAlignmentPoints; i++)
      {
        items.Add(Chain[i].ToString());
        items.Add(Curv[i].ToString());
      }

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    #endregion

    #region from_gwa_fns
    private bool AddSurface(string v)
    {
      GridSurfaceIndex = (int.TryParse(v, out var surfaceIndex) && surfaceIndex > 0) ? (int?)surfaceIndex : null;
      return true;
    }
    private bool AddNumAlignmentPoints(string v)
    {
      NumAlignmentPoints = (int.TryParse(v, out var value) && value > 0) ? (int?)value : null;
      return true;
    }

    private bool AddChain(string v)
    {
      if (double.TryParse(v, out var value) && value >= 0)
      {
        Chain.Add(value);
        return true;
      }
      return false;
    }

    private bool AddCurv(string v)
    {
      if (double.TryParse(v, out var value))
      {
        Curv.Add(value);
        return true;
      }
      return false;
    }
    #endregion
  }
}
