using System.Collections.Generic;
using DesktopUI2.Models.Settings;

namespace Archicad.Launcher;

public partial class ArchicadBinding
{
  public enum SettingSlugs
  {
    SendProperties = 0,
    SendListingParameters = 1,
    ReceiveParametric = 2
  }

  public static string[] settingSlugs =
  {
    "filter - properties",
    "filter - listing parameters",
    "receive - parametric"
  };

  public override List<ISetting> GetSettings()
  {
    return new List<ISetting>
    {
      new CheckBoxSetting
      {
        Slug = settingSlugs[(int)SettingSlugs.SendProperties],
        Name = "Send Properties",
        Icon = "Link",
        IsChecked = false,
        Description = "Send Properties created in the Property Manager"
      },
      new CheckBoxSetting
      {
        Slug = settingSlugs[(int)SettingSlugs.SendListingParameters],
        Name = "Send Listing Parameters",
        Icon = "Link",
        IsChecked = false,
        Description = "Send general and tool-specific parameters available for the fields of Element Schedules"
      },
      new CheckBoxSetting
      {
        Slug = settingSlugs[(int)SettingSlugs.ReceiveParametric],
        Name = "Receive parametric elements",
        Icon = "Link",
        IsChecked = false,
        Description = "Receive parametric elements where applicable"
      },
    };
  }
}
