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
    private List<ISetting> CurrentSettings { get; set; }

    public override List<ISetting> GetSettings()
    {
      List<string> unitOptions = new List<string>() { "in", "ft", "mm", "m" };

      return new List<ISetting>
      {
        new NumericUpDownWithComboBoxSetting {Slug = "model-tolerance", Name = "Model Tolerance (model units)", Icon ="LocationSearching" , Description = "Set the grid tolerance for the receiving of points in this Model",Value = "0.0", Values = unitOptions},
      };

    }

  }
}
