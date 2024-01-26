using ConnectorCSIShared.Util;
using DesktopUI2;
using DesktopUI2.Models.Settings;
using System.Collections.Generic;

namespace Speckle.ConnectorCSI.UI;

public partial class ConnectorBindingsCSI : ConnectorBindings
{
  // WARNING: These strings need to have the same value as the strings in ConverterCSIUtils
  public const string RESULTS_NODE_SLUG = "node-results";
  public const string RESULTS_1D_SLUG = "1d-results";
  public const string RESULTS_2D_SLUG = "2d-results";
  public const string RESULTS_LOAD_CASES_SLUG = "load-cases";

  public const string BEAM_FORCES = "Beam Forces";
  public const string BRACE_FORCES = "Brace Forces";
  public const string COLUMN_FORCES = "Column Forces";
  public const string OTHER_FORCES = "Other Forces";
  public const string FORCES = "Forces";
  public const string STRESSES = "Stresses";
  public const string DISPLACEMENTS = "Displacements";
  public const string VELOCITIES = "Velocities";
  public const string ACCELERATIONS = "Accelerations";

  public override List<ISetting> GetSettings()
  {
    return new List<ISetting>
    {
      new MultiSelectBoxSetting
      {
        Slug = RESULTS_LOAD_CASES_SLUG,
        Name = "Load Case Results To Send",
        Icon = "Link",
        Description =
          "Only the analytical results that belong to the load combinations selected here \nwill be sent to Speckle",
        Values = ResultUtils.GetNamesOfAllLoadCasesAndCombos(Model)
      },
      new MultiSelectBoxSetting
      {
        Slug = RESULTS_NODE_SLUG,
        Name = "Node Results To Send",
        Icon = "Link",
        Description = "Determines which node results are sent to Speckle",
        Values = new List<string>() { DISPLACEMENTS, FORCES, VELOCITIES, ACCELERATIONS }
      },
      new MultiSelectBoxSetting
      {
        Slug = RESULTS_1D_SLUG,
        Name = "1D Element Results To Send",
        Icon = "Link",
        Description = "Determines which 1D element results and sent to Speckle",
        Values = new List<string>() { BEAM_FORCES, BRACE_FORCES, COLUMN_FORCES, OTHER_FORCES }
      },
      new MultiSelectBoxSetting
      {
        Slug = RESULTS_2D_SLUG,
        Name = "2D Element Results To Send",
        Icon = "Link",
        Description = "Determines which 2D element results are computed and sent to Speckle",
        Values = new List<string>() { FORCES, STRESSES }
      },
    };
  }
}
