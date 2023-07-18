namespace RevitSharedResources.Interfaces
{
  /// <summary>
  /// Responsible for exposing and overwritting conversion settings
  /// </summary>
  public interface IConversionSettings
  {
    bool TryGetSettingBySlug(string slug, out string value);
    void SetSettingBySlug(string slug, string value);
  }
}
