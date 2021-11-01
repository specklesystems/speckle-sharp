namespace Speckle.GSA.API.CsvSchema
{
  public class CsvElem1d : CsvRecord
  {
    public virtual string PosR { get; set; }

    public virtual float? Ux { get; set; }

    public virtual float? Uy { get; set; }

    public virtual float? Uz { get; set; }

    public float? U { get => Magnitude(Ux, Uy, Uz); }

    public virtual float? Fx { get; set; }

    public virtual float? Fy { get; set; }

    public virtual float? Fz { get; set; }

    public float? F { get => Magnitude(Fx, Fy, Fz); }

    public virtual float? Mxx { get; set; }

    public virtual float? Myy { get; set; }

    public virtual float? Mzz { get; set; }

    public float? M { get => Magnitude(Mxx, Myy, Mzz); }

    public float? Fyz { get => Magnitude(Fy, Fz); }

    public float? Myz { get => Magnitude(Myy, Mzz); }
  }
}
