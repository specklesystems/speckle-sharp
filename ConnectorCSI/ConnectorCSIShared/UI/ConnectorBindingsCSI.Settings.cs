using System;
using System.Collections.Generic;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Speckle.Core.Logging;
using ConnectorCSI.Storage;
using System.Linq;
using System.Threading.Tasks;
using DesktopUI2.Models.Settings;

namespace Speckle.ConnectorCSI.UI
{
  public partial class ConnectorBindingsCSI : ConnectorBindings
  {
    public override List<ISetting> GetSettings()
    {
      return new List<ISetting>
      {
        new FieldBoxSetting {Slug = "model-tolerance", Name = "Model Tolerance (model units)", Icon ="LocationSearching" , Description = "Set the grid tolerance for the receiving of points in this Model"},
      };

    }

  }
}
