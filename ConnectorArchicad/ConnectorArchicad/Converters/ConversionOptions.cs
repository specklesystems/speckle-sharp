using System.Collections.Generic;
using DesktopUI2.Models.Settings;
using DynamicData;
using static Archicad.Launcher.ArchicadBinding;

namespace Archicad;

public class ConversionOptions
{
  public ConversionOptions(List<ISetting> settings)
  {
    foreach (var setting in settings)
    {
      switch (settingSlugs.IndexOf(setting.Slug))
      {
        case (int)SettingSlugs.SendProperties:
          SendProperties = ((CheckBoxSetting)setting).IsChecked;
          break;

        case (int)SettingSlugs.SendListingParameters:
          SendListingParameters = ((CheckBoxSetting)setting).IsChecked;
          break;

        case (int)SettingSlugs.ReceiveParametric:
          ReceiveParametric = ((CheckBoxSetting)setting).IsChecked;
          break;

        default:
          break;
      }
    }
  }

  public bool SendProperties { get; set; }
  public bool SendListingParameters { get; set; }
  public bool ReceiveParametric { get; set; }
}
