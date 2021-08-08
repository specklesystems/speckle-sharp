using System.Collections.Generic;
using System.Linq;


namespace Speckle.GSA.API.GwaSchema
{
  [GsaType(GwaKeyword.GRID_LINE, GwaSetCommandType.Set, true, GwaKeyword.GRID_SURFACE, GwaKeyword.LOAD_TITLE)]
  public class GsaGridLine : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public GridLineType Type;
    public double? XCoordinate;
    public double? YCoordinate;
    public double? Length;
    public double? Theta1;
    public double? Theta2;

    public GsaGridLine() : base()
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
      //GRID_LINE | num | name | arc | coor_x | coor_y | length | theta1 | theta2
      return FromGwaByFuncs(remainingItems, out var _, AddName, AddType, AddCoorX, AddCoorY, AddLength, AddTheta1, AddTheta2);
    }


    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //GRID_LINE | num | name | arc | coor_x | coor_y | length | theta1 | theta2
      AddItems(ref items, Name, AddType(), XCoordinate ?? 0, YCoordinate ?? 0, Length ?? 0, Theta1 ?? 0, Theta2 ?? 0);

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private string AddType()
    {
      return (Type == GridLineType.Line) ? "LINE" : "ARC";
    }
    #endregion

    #region from_gwa_fns
    private bool AddType(string v)
    {
      Type = v.ToUpperInvariant().Equals("LINE") ? GridLineType.Line : GridLineType.Arc;
      return true;
    }

    private bool AddCoorX(string v)
    {
      XCoordinate = (double.TryParse(v, out var coorX)) ? (double?)coorX : null;
      return true;
    }

    private bool AddCoorY(string v)
    {
      YCoordinate = (double.TryParse(v, out var coorY)) ? (double?)coorY : null;
      return true;
    }

    private bool AddLength(string v)
    {
      Length = (double.TryParse(v, out var length)) ? (double?)length : null;
      return true;
    }

    private bool AddTheta1(string v)
    {
      Theta1 = (double.TryParse(v, out var theta1)) ? (double?)theta1 : null;
      return true;
    }

    private bool AddTheta2(string v)
    {
      Theta2 = (double.TryParse(v, out var theta2)) ? (double?)theta2 : null;
      return true;
    }
    #endregion
  }
}
