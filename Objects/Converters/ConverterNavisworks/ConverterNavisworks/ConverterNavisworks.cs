using System;
using System.Collections.Generic;
using Autodesk.Navisworks.Api;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Version = System.Version;

namespace Objects.Converter.Navisworks
{
  public partial class ConverterNavisworks : ISpeckleConverter
  {
#if NAVMAN20
    public static string VersionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2023);
#elif NAVMAN19
        public static string VersionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2022);
#elif NAVMAN18
        public static string VersionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2021);
#elif NAVMAN17
        public static string VersionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2020);
#endif
    public ConverterNavisworks()
    {
      Version ver = System.Reflection.Assembly.GetAssembly(typeof(ConverterNavisworks)).GetName().Version;
    }

    public string Description => "Default Speckle Kit for Navisworks";

    public string Name => nameof(ConverterNavisworks);
    public string Author => "Speckle";
    public string WebsiteOrEmail => "https://speckle.systems";

    /// <summary>
    /// Keeps track of the conversion process
    /// </summary>
    public ProgressReport Report { get; private set; } = new ProgressReport();

    /// <summary>
    /// Decides what to do when an element being received already exists
    /// </summary>
    public ReceiveMode ReceiveMode { get; set; }

    public Document Doc { get; private set; }

    IEnumerable<string> ISpeckleConverter.GetServicedApplications() => new[] { VersionedAppName };

    /// <summary>
    /// Sets the application document that the converter is targeting
    /// </summary>
    /// <param name="doc">The current application document</param>
    public void SetContextDocument(object doc)
    {
      Doc = (Document)doc;

      // This sets the correct ElevationMode flag for model orientation.
      SetModelOrientationMode();
    }

    public List<ApplicationObject> ContextObjects { get; set; } = new List<ApplicationObject>();

    /// <summary>
    /// Some converters need to know which other objects are being converted, in order to sort relationships between them (ie, Revit). Use this method to set them.
    /// </summary>
    /// <param name="objects"></param>
    public void SetContextObjects(List<ApplicationObject> objects) => ContextObjects = objects;


    /// <summary>
    /// Some converters need to know which objects have been converted before in order to update them (ie, Revit). Use this method to set them.
    /// </summary>
    /// <param name="objects"></param>
    public void SetPreviousContextObjects(List<ApplicationObject> objects) => throw new NotImplementedException();


    /// <summary>
    /// Some converters need to be able to receive some settings to modify their internal behaviour (i.e. Rhino's Brep Meshing options). Use this method to set them.
    /// </summary>
    /// <param name="settings">The object representing the settings for your converter.</param>
    public void SetConverterSettings(object settings)
    {
    }
  }
}