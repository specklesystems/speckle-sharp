using Autodesk.Navisworks.Api.Interop;
using static Autodesk.Navisworks.Api.Interop.LcOpRegistry;
using static Autodesk.Navisworks.Api.Interop.LcUOption;

namespace Speckle.ConnectorNavisworks.NavisworksOptions;

/// <summary>
/// Manages the Autosave settings.
/// </summary>
public partial class NavisworksOptionsManager
{
  private bool _autosaveSetting;

  /// <summary>
  /// Updates the auto-save setting.
  /// </summary>
  /// <param name="enable">A boolean value indicating whether to enable or disable auto-save.</param>
  private void UpdateAutoSaveSetting(bool enable)
  {
    using var optionLock = new LcUOptionLock();
    var rootOptions = GetRoot(optionLock);
    var currentSetting = rootOptions.GetBoolean("general.autosave.enable");

    switch (enable)
    {
      // Autosave wasn't turned on at the time that the send operation was started
      case false when currentSetting == false:
        _autosaveSetting = false;
        return;
      // Autosave was turned on at the time that the send operation was started 
      case false:
        _autosaveSetting = true;
        rootOptions.SetBoolean("general.autosave.enable", false);
        break;
      // turn autosave back on if it was on before Send
      case true when _autosaveSetting:
        rootOptions.SetBoolean("general.autosave.enable", true);
        break;
    }

    SaveGlobalOptions();
  }

  /// <summary>
  /// Disables the auto-save feature.
  /// </summary>
  public void DisableAutoSave()
  {
    UpdateAutoSaveSetting(false);
  }

  /// <summary>
  /// Restores the auto-save setting to its original state after the send process.
  /// </summary>
  public void RestoreAutoSave()
  {
    UpdateAutoSaveSetting(true);
  }
}
