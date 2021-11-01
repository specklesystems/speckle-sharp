using CsvHelper.Configuration.Attributes;
using Speckle.GSA.API.CsvSchema;

namespace Speckle.ConnectorGSA.Results
{
  public class CsvNodeAnnotated: CsvNode
  {
    [Name("id")]
    public override int ElemId { get; set; }

    [Name("case_id")]
    public override string CaseId { get; set; }

    [Name("disp_x")]
    public override float? Ux { get; set; }

    [Name("disp_y")]
    public override float? Uy { get; set; }

    [Name("disp_z")]
    public override float? Uz { get; set; }

    [Name("disp_xx")]
    public override float? Rxx { get; set; }

    [Name("disp_yy")]
    public override float? Ryy { get; set; }

    [Name("disp_zz")]
    public override float? Rzz { get; set; }

    [Name("vel_x")]
    public override float? Vx { get; set; }

    [Name("vel_y")]
    public override float? Vy { get; set; }

    [Name("vel_z")]
    public override float? Vz { get; set; }

    [Name("vel_xx")]
    public override float? Vxx { get; set; }

    [Name("vel_yy")]
    public override float? Vyy { get; set; }

    [Name("vel_zz")]
    public override float? Vzz { get; set; }

    [Name("acc_x")]
    public override float? Ax { get; set; }

    [Name("acc_y")]
    public override float? Ay { get; set; }

    [Name("acc_z")]
    public override float? Az { get; set; }

    [Name("acc_xx")]
    public override float? Axx { get; set; }

    [Name("acc_yy")]
    public override float? Ayy { get; set; }

    [Name("acc_zz")]
    public override float? Azz { get; set; }

    [Name("reaction_x")]
    public override float? Fx_Reac { get; set; }

    [Name("reaction_y")]
    public override float? Fy_Reac { get; set; }

    [Name("reaction_z")]
    public override float? Fz_Reac { get; set; }

    [Name("reaction_xx")]
    public override float? Mxx_Reac { get; set; }

    [Name("reaction_yy")]
    public override float? Myy_Reac { get; set; }

    [Name("reaction_zz")]
    public override float? Mzz_Reac { get; set; }

    [Name("constraint_x")]
    public override float? Fx_Cons { get; set; }

    [Name("constraint_y")]
    public override float? Fy_Cons { get; set; }

    [Name("constraint_z")]
    public override float? Fz_Cons { get; set; }

    [Name("constraint_xx")]
    public override float? Mxx_Cons { get; set; }

    [Name("constraint_yy")]
    public override float? Myy_Cons { get; set; }

    [Name("constraint_zz")]
    public override float? Mzz_Cons { get; set; }
  }
}
