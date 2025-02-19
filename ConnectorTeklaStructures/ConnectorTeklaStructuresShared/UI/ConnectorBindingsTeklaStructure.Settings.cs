using System.Collections.Generic;
using DesktopUI2;
using DesktopUI2.Models.Settings;

namespace Speckle.ConnectorTeklaStructures.UI;

public partial class ConnectorBindingsTeklaStructures : ConnectorBindings
{
  public override List<ISetting> GetSettings()
  {
    return new List<ISetting>
    {
      new CheckBoxSetting
      {
        Slug = "recieve-objects-mesh",
        Name = "Receive Objects as Direct Mesh",
        Icon = "Link",
        IsChecked = false,
        Description = "Recieve the stream as a Meshes only"
      }
    };
  }
}
