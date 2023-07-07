using System;
using System.Collections.Generic;
using DesktopUI2.Models.Settings;
using DesktopUI2.Views.Controls.StreamEditControls;

namespace Archicad
{
  public class ConversionOptions
  {
    public ConversionOptions (List<ISetting> settings)
    {
      foreach (var setting in settings)
      {
        if (setting.Slug.Equals("receive - parametric"))
          if (bool.Parse(setting.Selection ?? "False"))
            ReceiveParametric = true;
      };
    }

    public bool ReceiveParametric { get; set; }
  }
}
