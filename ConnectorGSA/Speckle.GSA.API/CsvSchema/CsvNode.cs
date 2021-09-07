namespace Speckle.GSA.API.CsvSchema
{
  public class CsvNode : CsvRecord
  {
    public virtual float? Ux { get; set; }

    public virtual float? Uy { get; set; }

    public virtual float? Uz { get; set; }

    public float? U { get => Magnitude(Ux, Uy, Uz); }

    public virtual float? Rxx { get; set; }

    public virtual float? Ryy { get; set; }

    public virtual float? Rzz { get; set; }

    public float? R_Disp { get => Magnitude(Rxx, Ryy, Rzz); }

    public float? Uxy { get => Magnitude(Ux, Uy); }

    public virtual float? Vx { get; set; }

    public virtual float? Vy { get; set; }

    public virtual float? Vz { get; set; }

    public float? V { get => Magnitude(Vx, Vy, Vz); }

    public virtual float? Vxx { get; set; }

    public virtual float? Vyy { get; set; }

    public virtual float? Vzz { get; set; }

    public float? R_Vel { get => Magnitude(Vxx, Vyy, Vzz); }

    public virtual float? Ax { get; set; }

    public virtual float? Ay { get; set; }

    public virtual float? Az { get; set; }

    public float? A { get => Magnitude(Ax, Ay, Az); }

    public virtual float? Axx { get; set; }

    public virtual float? Ayy { get; set; }

    public virtual float? Azz { get; set; }

    public float? R_Acc { get => Magnitude(Axx, Ayy, Azz); }

    public virtual float? Fx_Reac { get; set; }

    public virtual float? Fy_Reac { get; set; }

    public virtual float? Fz_Reac { get; set; }

    public float? F_Reac { get => Magnitude(Fx_Reac, Fy_Reac, Fz_Reac); }

    public virtual float? Mxx_Reac { get; set; }

    public virtual float? Myy_Reac { get; set; }

    public virtual float? Mzz_Reac { get; set; }

    public float? M_Reac { get => Magnitude(Mxx_Reac, Myy_Reac, Mzz_Reac); }

    public virtual float? Fx_Cons { get; set; }

    public virtual float? Fy_Cons { get; set; }

    public virtual float? Fz_Cons { get; set; }

    public float? F_Cons { get => Magnitude(Fx_Cons, Fy_Cons, Fz_Cons); }

    public virtual float? Mxx_Cons { get; set; }

    public virtual float? Myy_Cons { get; set; }

    public virtual float? Mzz_Cons { get; set; }

    public float? M_Cons { get => Magnitude(Mxx_Cons, Myy_Cons, Mzz_Cons); }
  }
}
