using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using System.Collections.Generic;
using System.Text;
using DesktopUI2.Models.Settings;
using System.Linq;

namespace Speckle.ConnectorTeklaStructures.UI
{
  public partial class ConnectorBindingsTeklaStructures : ConnectorBindings
  {
    public override List<ISetting> GetSettings()
    {
      const string noMapping = "Never (default)";
      const string everyReceive = "Always";
      const string forNewTypes = "For New Types";
      List<string> mappingOptions = new List<string>() { noMapping, everyReceive, forNewTypes };

      return new List<ISetting>
      {
      new CheckBoxSetting {Slug = "recieve-objects-mesh", Name = "Receive Objects as Direct Mesh", Icon = "Link", IsChecked = false, Description = "Recieve the stream as a Meshes only"},
      new MappingSeting {Slug = "recieve-mappings", Name = "Custom Type Mapping", Icon ="LocationSearching", Values = mappingOptions, Description = "Sends or receives stream objects in relation to this document point"},

      };
    }
  }
}
