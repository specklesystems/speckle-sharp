using DesktopUI2.Models;
using DesktopUI2.Models.Settings;

namespace Speckle.ConnectorNavisworks.NavisworksOptions;

/// <summary>
/// Manages the Setting and Getting of Internal Navisworks options.
/// </summary>
public partial class NavisworksOptionsManager
{
  internal void InitializeManagerOptionsForSend(StreamState state)
  {
    var internalPropertySettings = state.Settings.Find(x => x.Slug == "internal-properties");
    var internalPropertyNames = state.Settings.Find(x => x.Slug == "internal-property-names");

    if (internalPropertySettings != null && ((CheckBoxSetting)internalPropertySettings).IsChecked)
    {
      ShowInternalProperties();
    }
    else
    {
      HideInternalProperties();
    }

    if (internalPropertyNames != null && ((CheckBoxSetting)internalPropertyNames).IsChecked)
    {
      UseInternalPropertyNames();
    }
    else
    {
      MaskInternalPropertyNames();
    }
  }
}
