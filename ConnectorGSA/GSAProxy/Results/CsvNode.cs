using CsvHelper.Configuration.Attributes;
using System;


namespace Speckle.ConnectorGSA.Results
{
  public class CsvNode : CsvRecord
  {
    [Name("disp_x")]
    public float? Ux { get; set; }

    [Name("disp_y")]
    public float? Uy { get; set; }

    [Name("disp_z")]
    public float? Uz { get; set; }

    public float? U { get => Magnitude(Ux, Uy, Uz); }

    [Name("disp_xx")]
    public float? Rxx { get; set; }

    [Name("disp_yy")]
    public float? Ryy { get; set; }

    [Name("disp_zz")]
    public float? Rzz { get; set; }

    public float? R_Disp { get => Magnitude(Rxx, Ryy, Rzz); }

    public float? Uxy { get => Magnitude(Ux, Uy); }

    [Name("vel_x")]
    public float? Vx { get; set; }

    [Name("vel_y")]
    public float? Vy { get; set; }

    [Name("vel_z")]
    public float? Vz { get; set; }

    public float? V { get => Magnitude(Vx, Vy, Vz); }

    [Name("vel_xx")]
    public float? Vxx { get; set; }

    [Name("vel_yy")]
    public float? Vyy { get; set; }

    [Name("vel_zz")]
    public float? Vzz { get; set; }

    public float? R_Vel { get => Magnitude(Vxx, Vyy, Vzz); }

    [Name("acc_x")]
    public float? Ax { get; set; }

    [Name("acc_y")]
    public float? Ay { get; set; }

    [Name("acc_z")]
    public float? Az { get; set; }

    public float? A { get => Magnitude(Ax, Ay, Az); }

    [Name("acc_xx")]
    public float? Axx { get; set; }

    [Name("acc_yy")]
    public float? Ayy { get; set; }

    [Name("acc_zz")]
    public float? Azz { get; set; }

    public float? R_Acc { get => Magnitude(Axx, Ayy, Azz); }

    [Name("reaction_x")]
    public float? Fx_Reac { get; set; }

    [Name("reaction_y")]
    public float? Fy_Reac { get; set; }

    [Name("reaction_z")]
    public float? Fz_Reac { get; set; }

    public float? F_Reac { get => Magnitude(Fx_Reac, Fy_Reac, Fz_Reac); }

    [Name("reaction_xx")]
    public float? Mxx_Reac { get; set; }

    [Name("reaction_yy")]
    public float? Myy_Reac { get; set; }

    [Name("reaction_zz")]
    public float? Mzz_Reac { get; set; }

    public float? M_Reac { get => Magnitude(Mxx_Reac, Myy_Reac, Mzz_Reac); }

    [Name("constraint_x")]
    public float? Fx_Cons { get; set; }

    [Name("constraint_y")]
    public float? Fy_Cons { get; set; }

    [Name("constraint_z")]
    public float? Fz_Cons { get; set; }

    public float? F_Cons { get => Magnitude(Fx_Cons, Fy_Cons, Fz_Cons); }

    [Name("constraint_xx")]
    public float? Mxx_Cons { get; set; }

    [Name("constraint_yy")]
    public float? Myy_Cons { get; set; }

    [Name("constraint_zz")]
    public float? Mzz_Cons { get; set; }

    public float? M_Cons { get => Magnitude(Mxx_Cons, Myy_Cons, Mzz_Cons); }
  }
}
