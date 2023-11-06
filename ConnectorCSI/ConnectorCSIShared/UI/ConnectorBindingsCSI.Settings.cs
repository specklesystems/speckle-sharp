using ConnectorCSIShared.Util;
using DesktopUI2;
using DesktopUI2.Models.Settings;
using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.ConnectorCSI.UI
{
  public partial class ConnectorBindingsCSI : ConnectorBindings
  {
    // WARNING: These strings need to have the same value as the strings in ConverterCSIUtils
    public const string ResultsNodeSlug = "node-results";
    public const string Results1dSlug = "1d-results";
    public const string Results2dSlug = "2d-results";

    public const string BeamForces = "Beam Forces";
    public const string BraceForces = "Brace Forces";
    public const string ColumnForces = "Column Forces";
    public const string OtherForces = "Other Forces";
    public const string Forces = "Forces";
    public const string Stresses = "Stresses";
    public const string Displacements = "Displacements";
    public const string Velocities = "Velocities";
    public const string Accelerations = "Accelerations";
    public override List<ISetting> GetSettings()
    {
      return new List<ISetting>
      {
        new MultiSelectBoxSetting
        {
          Slug = "load-cases",
          Name = "Load Case Results To Send",
          Icon = "Link",
          Description = "Only the analytical results that belong to the load combinations selected here \nwill be sent to Speckle",
          Values = ResultUtils.GetNamesOfAllLoadCasesAndCombos(Model)
        },
        
        new MultiSelectBoxSetting
        {
          Slug = ResultsNodeSlug,
          Name = "Node Results To Send",
          Icon = "Link",
          Description = "Determines which node results are sent to Speckle",
          Values = new List<string>() { Displacements, Forces, Velocities, Accelerations }
        },
        
        new MultiSelectBoxSetting
        {
          Slug = Results1dSlug,
          Name = "1D Element Results To Send",
          Icon = "Link",
          Description = "Determines which 1D element results and sent to Speckle",
          Values = new List<string>() { BeamForces, BraceForces, ColumnForces, OtherForces }
        },
        
        new MultiSelectBoxSetting
        {
          Slug = Results2dSlug,
          Name = "2D Element Results To Send",
          Icon = "Link",
          Description = "Determines which 2D element results are computed and sent to Speckle",
          Values = new List<string>() { Forces, Stresses }
        },
      };
    }
  }
}
