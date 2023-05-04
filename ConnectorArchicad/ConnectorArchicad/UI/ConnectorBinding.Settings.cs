using System.Collections.Generic;
using System.Linq;
using DesktopUI2.Models.Settings;

namespace Archicad.Launcher
{
  public partial class ArchicadBinding
  {
    public override List<ISetting> GetSettings()
    {
      return new List<ISetting>
      {
        new CheckBoxSetting {Slug = "receive - parametric", Name = "Receive parametric elements", Icon = "Link", IsChecked = true, Description = "Receive parametric elements where applicable"},
      };
    }
  }
}
