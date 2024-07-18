using Autodesk.Navisworks.Api.Interop;
using static Autodesk.Navisworks.Api.Interop.LcOpRegistry;
using static Autodesk.Navisworks.Api.Interop.LcUOption;

namespace Speckle.ConnectorNavisworks.NavisworksOptions;

/// <summary>
/// Manages the Property Display settings.
/// </summary>
public partial class NavisworksOptionsManager
{
  private bool _internalPropertyDisplaySetting;
  private bool _useInternalPropertyNamesSetting;

  /// <summary>
  /// Updates the specified option setting.
  /// </summary>
  /// <param name="optionName">The name of the option to update.</param>
  /// <param name="optionSetting">The current value of the option setting.</param>
  /// <param name="enable">A boolean value indicating whether to enable or disable the option.</param>
  private void UpdateOptionSetting(string optionName, ref bool optionSetting, bool enable)
  {
    using var optionLock = new LcUOptionLock();
    var rootOptions = GetRoot(optionLock);

    var currentSetting = rootOptions.GetBoolean(optionName);
    if (currentSetting == enable)
    {
      return;
    }

    optionSetting = currentSetting;

    rootOptions.SetBoolean(optionName, enable);
    SaveGlobalOptions();
  }

  /// <summary>
  /// Updates the internal property display setting.
  /// </summary>
  private void UpdateInternalPropertySetting() =>
    UpdateOptionSetting(
      "interface.developer.show_properties",
      ref _internalPropertyDisplaySetting,
      _internalPropertyDisplaySetting
    );

  /// <summary>
  /// Updates the internal property display setting.
  /// </summary>
  /// <param name="enable">A boolean value indicating whether to enable or disable the internal property display.</param>
  private void UpdateInternalPropertySetting(bool enable) =>
    UpdateOptionSetting("interface.developer.show_properties", ref _internalPropertyDisplaySetting, enable);

  /// <summary>
  /// Updates the internal property name setting.
  /// </summary>
  private void UpdateInternalPropertyNameSetting() =>
    UpdateOptionSetting(
      "interface.developer.show_property_internal_names",
      ref _useInternalPropertyNamesSetting,
      _useInternalPropertyNamesSetting
    );

  /// <summary>
  /// Updates the internal property name setting.
  /// </summary>
  /// <param name="enable">A boolean value indicating whether to enable or disable the internal property names.</param>
  private void UpdateInternalPropertyNameSetting(bool enable) =>
    UpdateOptionSetting(
      "interface.developer.show_property_internal_names",
      ref _useInternalPropertyNamesSetting,
      enable
    );

  /// <summary>
  /// Shows internal properties.
  /// </summary>
  public void ShowInternalProperties() => UpdateInternalPropertySetting(true);

  /// <summary>
  /// Hides internal properties.
  /// </summary>
  public void HideInternalProperties() => UpdateInternalPropertySetting(false);

  /// <summary>
  /// Uses internal property names.
  /// </summary>
  public void UseInternalPropertyNames() => UpdateInternalPropertyNameSetting(true);

  /// <summary>
  /// Masks internal property names.
  /// </summary>
  public void MaskInternalPropertyNames() => UpdateInternalPropertyNameSetting(false);

  /// <summary>
  /// Restores the internal properties display to its original state after the send process.
  /// </summary>
  public void RestoreInternalPropertiesDisplay()
  {
    UpdateInternalPropertySetting();
    UpdateInternalPropertyNameSetting();
  }
}
