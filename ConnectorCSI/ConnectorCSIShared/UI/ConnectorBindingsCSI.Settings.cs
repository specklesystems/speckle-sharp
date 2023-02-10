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
    readonly string SendNodeResults = "sendNodeResults";
    readonly string Send1DResults = "send1DResults";
    readonly string Send2DResults = "send2DResults";
    public override List<ISetting> GetSettings()
    {
      return new List<ISetting>
      {
        new CheckBoxSetting {Slug = SendNodeResults, Name = "Send Node Analysis Results", Icon ="Link", IsChecked= false, Description = "Include node analysis results with object data when sending to Speckle. This will make the sending process take more time."},
        new CheckBoxSetting {Slug = Send1DResults, Name = "Send 1D Analysis Results", Icon ="Link", IsChecked= false, Description = "Include 1D analysis results with object data when sending to Speckle. This will make the sending process take more time."},
        new CheckBoxSetting {Slug = Send2DResults, Name = "Send 2D Analysis Results", Icon ="Link", IsChecked= false, Description = "Include 2D analysis results with object data when sending to Speckle. This will make the sending process take MUCH more time."},
      };
    }
  }
}
