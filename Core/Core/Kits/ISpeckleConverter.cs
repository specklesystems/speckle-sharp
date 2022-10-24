using System.Collections.Generic;
using Speckle.Core.Models;

namespace Speckle.Core.Kits
{
  public interface ISpeckleConverter
  {
    string Description { get; }
    string Name { get; }
    string Author { get; }
    string WebsiteOrEmail { get; }

    /// <summary>
    /// Keeps track of the conversion process
    /// </summary>
    public ProgressReport Report { get; }

    /// <summary>
    /// Decides what to do when an element being received already exists
    /// </summary>
    public ReceiveMode ReceiveMode { get; set; }


    /// <summary>
    /// Converts a native object to a Speckle one
    /// </summary>
    /// <param name="object">Native object to convert</param>
    /// <returns></returns>
    public Base ConvertToSpeckle(object @object);

    /// <summary>
    /// Converts a list of objects to Speckle.
    /// </summary>
    /// <param name="objects"></param>
    /// <returns></returns>
    public List<Base> ConvertToSpeckle(List<object> objects);

    /// <summary>
    /// Checks if it can convert a native object to a Speckle one
    /// </summary>
    /// <param name="object">Native object to convert</param>
    /// <returns></returns>
    public bool CanConvertToSpeckle(object @object);

    /// <summary>
    /// Converts a Speckle object to a native one
    /// </summary>
    /// <param name="object">Speckle object to convert</param>
    /// <returns></returns>
    public object ConvertToNative(Base @object);

    /// <summary>
    /// Converts a list of Speckle objects to a native ones.
    /// </summary>
    /// <param name="objects"></param>
    /// <returns></returns>
    public List<object> ConvertToNative(List<Base> objects);

    /// <summary>
    /// Checks if it can convert a Speckle object to a native one
    /// </summary>
    /// <param name="object">Speckle object to convert</param>
    /// <returns></returns>
    public bool CanConvertToNative(Base @object);

    /// <summary>
    /// Returns a list of applications serviced by this converter
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> GetServicedApplications();

    /// <summary>
    /// Sets the application document that the converter is targeting
    /// </summary>
    /// <param name="doc">The current application document</param>
    public void SetContextDocument(object doc);

    /// <summary>
    /// Some converters need to know which other objects are being converted, in order to sort relationships between them (ie, Revit). Use this method to set them.
    /// </summary>
    /// <param name="objects"></param>
    public void SetContextObjects(List<ApplicationObject> objects);

    /// <summary>
    /// Some converters need to know which objects have been converted before in order to update them (ie, Revit). Use this method to set them.
    /// </summary>
    /// <param name="objects"></param>
    public void SetPreviousContextObjects(List<ApplicationObject> objects);

    /// <summary>
    /// Some converters need to be able to receive some settings to modify their internal behaviour (i.e. Rhino's Brep Meshing options). Use this method to set them.
    /// </summary>
    /// <param name="settings">The object representing the settings for your converter.</param>
    public void SetConverterSettings(object settings);

  }

  // NOTE: Do not change the order of the existing ones
  /// <summary>
  /// Receive modes indicate what to do and not do when receiving objects
  /// </summary>
  public enum ReceiveMode
  {
    /// <summary>
    /// Attemts updating previously received objects by ID, deletes previously received objects that do not exist anymore and creates new ones
    /// </summary>
    Update,
    /// <summary>
    /// Always creates new objects
    /// </summary>
    Create,
    /// <summary>
    /// Ignores updating previously received objects and does not attempt updating or deleting them, creates new objects
    /// </summary>
    Ignore
  }
}
