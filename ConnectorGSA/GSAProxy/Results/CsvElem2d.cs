using CsvHelper.Configuration.Attributes;
using System;

namespace Speckle.ConnectorGSA.Results
{
  public class CsvElem2d : CsvRecord
  {
    [Name("position_r")]
    public float? PosR { get; set; }

    [Name("position_s")]
    public float? PosS { get; set; }

    [Name("disp_x")]
    public float? Ux { get; set; }

    [Name("disp_y")]
    public float? Uy { get; set; }

    [Name("disp_z")]
    public float? Uz { get; set; }

    public float? U { get => Magnitude(Ux, Uy, Uz); }

    [Name("moment_xx")]
    public float? Mx { get; set; }

    [Name("moment_yy")]
    public float? My { get; set; }

    [Name("moment_xy")]
    public float? Mxy { get; set; }

    //mx+mxy
    public float? Mx_Mxy { get => MomentResult(Mx, Mxy); }
    
    //my+myx
    public float? My_Myx { get => MomentResult(My, Mxy); }

    [Name("force_xx")]
    public float? Nx { get; set; }

    [Name("force_yy")]
    public float? Ny { get; set; }

    [Name("force_xy")]
    public float? Nxy { get; set; }

    [Name("shear_x")]
    public float? Qx { get; set; }

    [Name("shear_y")]
    public float? Qy { get; set; }

    [Name("stress_bottom_xx")]
    public float? Xx_b { get; set; }

    [Name("stress_bottom_yy")]
    public float? Yy_b { get; set; }

    [Name("stress_bottom_zz")]
    public float? Zz_b { get; set; }

    [Name("stress_bottom_xy")]
    public float? Xy_b { get; set; }

    [Name("stress_bottom_yz")]
    public float? Yz_b { get; set; }

    [Name("stress_bottom_zx")]
    public float? Zx_b { get; set; }

    [Name("stress_middle_xx")]
    public float? Xx_m { get; set; }

    [Name("stress_middle_yy")]
    public float? Yy_m { get; set; }

    [Name("stress_middle_zz")]
    public float? Zz_m { get; set; }

    [Name("stress_middle_xy")]
    public float? Xy_m { get; set; }

    [Name("stress_middle_yz")]
    public float? Yz_m { get; set; }

    [Name("stress_middle_zx")]
    public float? Zx_m { get; set; }

    [Name("stress_top_xx")]
    public float? Xx_t { get; set; }

    [Name("stress_top_yy")]
    public float? Yy_t { get; set; }

    [Name("stress_top_zz")]
    public float? Zz_t { get; set; }

    [Name("stress_top_xy")]
    public float? Xy_t { get; set; }

    [Name("stress_top_yz")]
    public float? Yz_t { get; set; }

    [Name("stress_top_zx")]
    public float? Zx_t { get; set; }

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
