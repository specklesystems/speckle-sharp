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
    _autosaveSetting = rootOptions.GetBoolean("general.autosave.enable");

    if (_autosaveSetting == enable)
      return;

    rootOptions.SetBoolean("general.autosave.enable", enable);
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
