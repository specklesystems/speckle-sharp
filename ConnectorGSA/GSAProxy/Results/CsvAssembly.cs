using CsvHelper.Configuration.Attributes;
using System;

namespace Speckle.ConnectorGSA.Results
{
  public class CsvAssembly : CsvRecord
  {
    [Name("position_r")]
    public float? PosR { get; set; }

    [Name("force_x")]
    public float? Fx { get; set; }

    [Name("force_y")]
    public float? Fy { get; set; }

    [Name("force_z")]
    public float? Fz { get; set; }

    public float? Frc { get => Magnitude(Fx, Fy, Fz); }

    [Name("moment_x")]
    public float? Mxx { get; set; }

    [Name("moment_y")]
    public float? Myy { get; set; }

    [Name("moment_z")]
    public float? Mzz { get; set; }

    public float? Mom { get => Magnitude(Mxx, Myy, Mzz); }
  }
}
