using CsvHelper.Configuration.Attributes;
using Speckle.GSA.API.CsvSchema;

namespace Speckle.ConnectorGSA.Results
{
  public class CsvElem1dAnnotated : CsvElem1d
  {
    [Name("id")]
    public override int ElemId { get; set; }

    [Name("case_id")]
    public override string CaseId { get; set; }

    [Name("position_r")]
    public override string PosR { get; set; }

    [Name("disp_x")]
    public override float? Ux { get; set; }

    [Name("disp_y")]
    public override float? Uy { get; set; }

    [Name("disp_z")]
    public override float? Uz { get; set; }

    [Name("force_x")]
    public override float? Fx { get; set; }

    [Name("force_y")]
    public override float? Fy { get; set; }

    [Name("force_z")]
    public override float? Fz { get; set; }

    [Name("moment_x")]
    public override float? Mxx { get; set; }

    [Name("moment_y")]
    public override float? Myy { get; set; }

    [Name("moment_z")]
    public override float? Mzz { get; set; }
  }
}
