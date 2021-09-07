using CsvHelper.Configuration.Attributes;
using Speckle.GSA.API.CsvSchema;
using System;

namespace Speckle.ConnectorGSA.Results
{
  public class CsvElem2dAnnotated : CsvElem2d
  {
    [Name("id")]
    public override int ElemId { get; set; }

    [Name("case_id")]
    public override string CaseId { get; set; }

    [Name("position_r")]
    public override float? PosR { get; set; }

    [Name("position_s")]
    public override float? PosS { get; set; }

    [Name("disp_x")]
    public override float? Ux { get; set; }

    [Name("disp_y")]
    public override float? Uy { get; set; }

    [Name("disp_z")]
    public override float? Uz { get; set; }

    [Name("moment_xx")]
    public override float? Mx { get; set; }

    [Name("moment_yy")]
    public override float? My { get; set; }

    [Name("moment_xy")]
    public override float? Mxy { get; set; }

    [Name("force_xx")]
    public override float? Nx { get; set; }

    [Name("force_yy")]
    public override float? Ny { get; set; }

    [Name("force_xy")]
    public override float? Nxy { get; set; }

    [Name("shear_x")]
    public override float? Qx { get; set; }

    [Name("shear_y")]
    public override float? Qy { get; set; }

    [Name("stress_bottom_xx")]
    public override float? Xx_b { get; set; }

    [Name("stress_bottom_yy")]
    public override float? Yy_b { get; set; }

    [Name("stress_bottom_zz")]
    public override float? Zz_b { get; set; }

    [Name("stress_bottom_xy")]
    public override float? Xy_b { get; set; }

    [Name("stress_bottom_yz")]
    public override float? Yz_b { get; set; }

    [Name("stress_bottom_zx")]
    public override float? Zx_b { get; set; }

    [Name("stress_middle_xx")]
    public override float? Xx_m { get; set; }

    [Name("stress_middle_yy")]
    public override float? Yy_m { get; set; }

    [Name("stress_middle_zz")]
    public override float? Zz_m { get; set; }

    [Name("stress_middle_xy")]
    public override float? Xy_m { get; set; }

    [Name("stress_middle_yz")]
    public override float? Yz_m { get; set; }

    [Name("stress_middle_zx")]
    public override float? Zx_m { get; set; }

    [Name("stress_top_xx")]
    public override float? Xx_t { get; set; }

    [Name("stress_top_yy")]
    public override float? Yy_t { get; set; }

    [Name("stress_top_zz")]
    public override float? Zz_t { get; set; }

    [Name("stress_top_xy")]
    public override float? Xy_t { get; set; }

    [Name("stress_top_yz")]
    public override float? Yz_t { get; set; }

    [Name("stress_top_zx")]
    public override float? Zx_t { get; set; }
  }
}
