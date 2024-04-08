using System;
using System.Collections.Generic;
using Autodesk.Navisworks.Api;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Converter.Navisworks;

// ReSharper disable once UnusedType.Global
public partial class ConverterNavisworks : ISpeckleConverter
{
#if NAVMAN22
  private static readonly string s_versionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2025);
#elif NAVMAN21
  private static readonly string s_versionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2024);
#elif NAVMAN20
  private static readonly string s_versionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2023);
#elif NAVMAN19
  private static readonly string s_versionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2022);
#elif NAVMAN18
  private static readonly string s_versionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2021);
#elif NAVMAN17
  private static readonly string s_versionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2020);
#endif

  public string Description => "Default Speckle Kit for Navisworks";

  public string Name => nameof(ConverterNavisworks);

  public string Author => "Speckle";

  public string WebsiteOrEmail => "https://speckle.systems";

  /// <summary>
  ///   Keeps track of the conversion process
  /// </summary>
  public ProgressReport Report { get; } = new();

  /// <inheritdoc />
  public ReceiveMode ReceiveMode { get; set; }

  private static Document Doc { get; set; }

  public IEnumerable<string> GetServicedApplications() => new[] { s_versionedAppName };

  public void SetContextDocument(object doc)
  {
    if (doc != null && doc is not Document)
    {
      throw new ArgumentException("Only Navisworks Document types are supported.");
    }

    if (Doc == null && doc != null)
    {
      Doc = (Document)doc;
    }

    if (Doc == null && doc == null)
    {
      Doc = Application.ActiveDocument;
    }

    // This sets or resets the correct IsUpright flag for model orientation.
    // Needs to be called every time a Send is initiated to reflect the options
    SetModelOrientationMode();
    SetModelBoundingBox();
    SetTransformVector3D();
  }

  private List<ApplicationObject> _contextObjects = new();

  public IReadOnlyList<ApplicationObject> ContextObjects => _contextObjects;

  /// <inheritdoc />
  public void SetContextObjects(List<ApplicationObject> objects) =>
    _contextObjects = objects ?? throw new ArgumentNullException(nameof(objects));

  /// <inheritdoc />
  public void SetPreviousContextObjects(List<ApplicationObject> objects) => throw new NotImplementedException();

  /// <inheritdoc />
  public void SetConverterSettings(object settings)
  {
    if (settings is not Dictionary<string, string> newSettings)
    {
      return;
    }

    foreach (var key in newSettings.Keys)
    {
      if (Settings.TryGetValue(key, out string _))
      {
        Settings[key] = newSettings[key];
      }
      else
      {
        Settings.Add(key, newSettings[key]);
      }
    }
  }
}
