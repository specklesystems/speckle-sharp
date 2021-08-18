using CsvHelper.Configuration.Attributes;
using System;


namespace Speckle.ConnectorGSA.Results
{
  public class CsvElem1d : CsvRecord
  {
    [Name("disp_x")]
    public float? Ux { get; set; }

    [Name("disp_y")]
    public float? Uy { get; set; }

    [Name("disp_z")]
    public float? Uz { get; set; }

    public float? U { get => Magnitude(Ux, Uy, Uz); }

    [Name("force_x")]
    public float? Fx { get; set; }

    [Name("force_y")]
    public float? Fy { get; set; }

    [Name("force_z")]
    public float? Fz { get; set; }

    public float? F { get => Magnitude(Fx, Fy, Fz); }

    [Name("moment_x")]
    public float? Mxx { get; set; }

    [Name("moment_y")]
    public float? Myy { get; set; }

    [Name("moment_z")]
    public float? Mzz { get; set; }

    public float? M { get => Magnitude(Mxx, Myy, Mzz); }

    public float? Fyz { get => Magnitude(Fy, Fz); }

    public float? Myz { get => Magnitude(Myy, Mzz); }
  }
}
