using System;

namespace Speckle.GSA.API.CsvSchema
{
  public class CsvElem2d : CsvRecord
  {
    public virtual float? PosR { get; set; }

    public virtual float? PosS { get; set; }

    public virtual float? Ux { get; set; }

    public virtual float? Uy { get; set; }

    public virtual float? Uz { get; set; }

    public float? U { get => Magnitude(Ux, Uy, Uz); }

    public virtual float? Mx { get; set; }

    public virtual float? My { get; set; }

    public virtual float? Mxy { get; set; }

    //mx+mxy
    public float? Mx_Mxy { get => MomentResult(Mx, Mxy); }

    //my+myx
    public float? My_Myx { get => MomentResult(My, Mxy); }

    public virtual float? Nx { get; set; }

    public virtual float? Ny { get; set; }

    public virtual float? Nxy { get; set; }

    public virtual float? Qx { get; set; }

    public virtual float? Qy { get; set; }

    public virtual float? Xx_b { get; set; }

    public virtual float? Yy_b { get; set; }

    public virtual float? Zz_b { get; set; }

    public virtual float? Xy_b { get; set; }

    public virtual float? Yz_b { get; set; }

    public virtual float? Zx_b { get; set; }

    public virtual float? Xx_m { get; set; }

    public virtual float? Yy_m { get; set; }

    public virtual float? Zz_m { get; set; }

    public virtual float? Xy_m { get; set; }

    public virtual float? Yz_m { get; set; }

    public virtual float? Zx_m { get; set; }

    public virtual float? Xx_t { get; set; }

    public virtual float? Yy_t { get; set; }

    public virtual float? Zz_t { get; set; }

    public virtual float? Xy_t { get; set; }

    public virtual float? Yz_t { get; set; }

    public virtual float? Zx_t { get; set; }

    public bool IsVertex
    {
      get
      {
        if (PosR.HasValue)
        {
          var rounded = Math.Round(PosR.Value, 1);
          return (rounded == 0 || rounded == 1);
        }
        return false;
      }
    }
  }
}
