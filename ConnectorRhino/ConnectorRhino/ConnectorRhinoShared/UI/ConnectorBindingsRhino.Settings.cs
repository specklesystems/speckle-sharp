using System.Collections.Generic;
using DesktopUI2;
using DesktopUI2.Models.Settings;
using Rhino;
using Rhino.DocObjects.Tables;

namespace SpeckleRhino;

public partial class ConnectorBindingsRhino : ConnectorBindings
{
  const string defaultValue = "Default";
  const string mergeCoplanar = "Merge Coplanar Faces";

  public override List<ISetting> GetSettings()
  {
    List<string> meshImportOptions = new List<string>() { defaultValue, mergeCoplanar };

    return new List<ISetting>
    {
      new ListBoxSetting
      {
        Slug = "receive-mesh",
        Name = "Mesh Import Method",
        Icon = "ChartTimelineVarient",
        Values = meshImportOptions,
        Selection = defaultValue,
        Description = "Determines the method of importing meshes"
      }
    };
  }

  /// <summary>
  /// Converts the settings to (setting slug, setting selection) key value pairs
  /// </summary>
  /// <returns></returns>
  public Dictionary<string, string> GetSettingsDict(List<ISetting>? currentSettings)
  {
    var settings = new Dictionary<string, string>();
    foreach (var setting in currentSettings)
      settings.Add(setting.Slug, setting.Selection);
    return settings;
  }
}
