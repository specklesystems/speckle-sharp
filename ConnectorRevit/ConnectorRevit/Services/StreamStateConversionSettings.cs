using System.Collections.Generic;
using DesktopUI2.Models;
using RevitSharedResources.Interfaces;

namespace ConnectorRevit.Services
{
  /// <summary>
  /// implements <see cref="IConversionSettings"/>
  /// </summary>
  public class StreamStateConversionSettings : IConversionSettings
  {
    public StreamStateConversionSettings(IEntityProvider<StreamState> streamStateProvider)
    {
      foreach (var setting in streamStateProvider.Entity.Settings)
        settingsDict.Add(setting.Slug, setting.Selection);
    }

    private Dictionary<string, string> settingsDict = new();

    public bool TryGetSettingBySlug(string slug, out string value)
    {
      return settingsDict.TryGetValue(slug, out value);
    }

    public void SetSettingBySlug(string slug, string value)
    {
      settingsDict[slug] = value;
    }
  }
}
