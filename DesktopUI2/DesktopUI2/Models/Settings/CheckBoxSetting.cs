using Avalonia.Controls;
using DesktopUI2.Views.Settings;

namespace DesktopUI2.Models.Settings
{
  public class CheckBoxSetting : ISetting
  {
    public string Type => typeof(CheckBoxSetting).ToString();
    public string Name { get; set; }
    public string Slug { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }
    public bool IsChecked { get; set; }
    public string Selection { get; set; }
    public UserControl View { get; set; } = new CheckBoxSettingView();
    public string Summary { get; set; }

    public void ResetView()
    {
      View = new CheckBoxSettingView();
    }
  }
}
