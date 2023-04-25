using System;
using System.Collections.Generic;
using Autodesk.Navisworks.Api;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Converter.Navisworks;

// ReSharper disable once UnusedType.Global
public partial class ConverterNavisworks : ISpeckleConverter
{
#if NAVMAN21
    public readonly static string VersionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2024);
#elif NAVMAN20
  public readonly static string VersionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2023);
#elif NAVMAN19
    public readonly static string VersionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2022);
#elif NAVMAN18
    public readonly static string VersionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2021);
#elif NAVMAN17
  private readonly static string VersionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2020);
#endif

  public string Description => "Default Speckle Kit for Navisworks";

  public string Name => nameof(ConverterNavisworks);

  public string Author => "Speckle";

  public string WebsiteOrEmail => "https://speckle.systems";

  /// <summary>
  ///   Keeps track of the conversion process
  /// </summary>
  public ProgressReport Report { get; } = new();

  /// <summary>
  ///   Decides what to do when an element being received already exists
  /// </summary>
  public ReceiveMode ReceiveMode { get; set; }

  private static Document Doc { get; set; }

  public IEnumerable<string> GetServicedApplications()
  {
    return new[] { VersionedAppName };
  }

  /// <summary>
  ///   Sets the application document that the converter is targeting
  /// </summary>
  /// <param name="doc">The current application document</param>
  public void SetContextDocument(object doc)
  {
    Doc = (Document)doc;
    // This sets the correct ElevationMode flag for model orientation.
    SetModelOrientationMode();
    SetModelBoundingBox();
    SetTransformVector3D();
  }

  private List<ApplicationObject> _contextObjects = new();
  public IReadOnlyList<ApplicationObject> ContextObjects => _contextObjects;

  // Add a new method to allow adding items to the list.
  public void AddContextObject(ApplicationObject contextObject)
  {
    _contextObjects.Add(contextObject);
  }

  /// <summary>
  ///   Some converters need to know which other objects are being converted, in order to sort relationships between them
  ///   (ie, Revit). Use this method to set them.
  /// </summary>
  /// <param name="objects"></param>
  public void SetContextObjects(List<ApplicationObject> objects)
  {
    _contextObjects = objects ?? throw new ArgumentNullException(nameof(objects));
  }

  /// <summary>
  ///   Some converters need to know which objects have been converted before in order to update them (ie, Revit). Use this
  ///   method to set them.
  /// </summary>
  /// <param name="objects"></param>
  public void SetPreviousContextObjects(List<ApplicationObject> objects)
  {
    throw new NotImplementedException();
  }

  public void SetConverterSettings(object settings)
  {
    if (settings is not Dictionary<string, string> newSettings)
      return;

    foreach (var key in newSettings.Keys)
      if (Settings.TryGetValue(key, out string _))
        Settings[key] = newSettings[key];
      else
        Settings.Add(key, newSettings[key]);
  }
}
