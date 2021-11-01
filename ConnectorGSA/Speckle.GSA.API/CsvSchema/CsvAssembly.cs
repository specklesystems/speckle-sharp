namespace Speckle.GSA.API.CsvSchema
{
  public class CsvAssembly : CsvRecord
  {
    public virtual float? PosR { get; set; }

    public virtual float? Fx { get; set; }

    public virtual float? Fy { get; set; }

    public virtual float? Fz { get; set; }

    public float? Frc { get => Magnitude(Fx, Fy, Fz); }

    public virtual float? Mxx { get; set; }

    public virtual float? Myy { get; set; }

    public virtual float? Mzz { get; set; }

    public float? Mom { get => Magnitude(Mxx, Myy, Mzz); }
  }
}
