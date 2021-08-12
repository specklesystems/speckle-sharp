using Speckle.GSA.API.GwaSchema;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  [GsaType(GwaKeyword.GRID_LINE, GwaSetCommandType.Set, true, GwaKeyword.GRID_SURFACE, GwaKeyword.LOAD_TITLE)]
  public class GsaGridLineParser : GwaParser<GsaGridLine>
  {
    public GsaGridLineParser(GsaGridLine gsaGridLine) : base(gsaGridLine) { }
    public GsaGridLineParser() : base(new GsaGridLine()) { }

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
      AddItems(ref items, record.Name, AddType(), record.XCoordinate ?? 0, record.YCoordinate ?? 0, record.Length ?? 0, 
        record.Theta1 ?? 0, record.Theta2 ?? 0);

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private string AddType()
    {
      return (record.Type == GridLineType.Line) ? "LINE" : "ARC";
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
      record.Type = v.ToUpperInvariant().Equals("LINE") ? GridLineType.Line : GridLineType.Arc;
      return true;
    }

    private bool AddCoorX(string v)
    {
      record.XCoordinate = (double.TryParse(v, out var coorX)) ? (double?)coorX : null;
      return true;
    }

    private bool AddCoorY(string v)
    {
      record.YCoordinate = (double.TryParse(v, out var coorY)) ? (double?)coorY : null;
      return true;
    }

    private bool AddLength(string v)
    {
      record.Length = (double.TryParse(v, out var length)) ? (double?)length : null;
      return true;
    }

    private bool AddTheta1(string v)
    {
      record.Theta1 = (double.TryParse(v, out var theta1)) ? (double?)theta1 : null;
      return true;
    }

    private bool AddTheta2(string v)
    {
      record.Theta2 = (double.TryParse(v, out var theta2)) ? (double?)theta2 : null;
      return true;
    }
    #endregion
  }
}
