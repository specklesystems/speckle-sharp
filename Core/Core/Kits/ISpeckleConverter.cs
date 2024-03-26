#nullable disable
using System.Collections.Generic;
using Speckle.Core.Models;

namespace Speckle.Core.Kits;

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
  /// <param name="value">Native object to convert</param>
  /// <returns></returns>
  /// <exception cref="System.Exception"></exception>
  public Base ConvertToSpeckle(object value);

  /// <summary>
  /// Converts a list of objects to Speckle.
  /// </summary>
  /// <param name="values"></param>
  /// <returns></returns>
  /// <exception cref="System.Exception"></exception>
  public List<Base> ConvertToSpeckle(List<object> values);

  /// <summary>
  /// Checks if it can convert a native object to a Speckle one
  /// </summary>
  /// <param name="value">Native object to convert</param>
  /// <returns></returns>
  public bool CanConvertToSpeckle(object value);

  /// <summary>
  /// Converts a Speckle object to a native one
  /// </summary>
  /// <param name="value">Speckle object to convert</param>
  /// <returns></returns>
  /// <exception cref="System.Exception"></exception>
  public object ConvertToNative(Base value);

  /// <summary>
  /// Converts a list of Speckle objects to a native ones.
  /// </summary>
  /// <param name="values"></param>
  /// <returns></returns>
  /// <exception cref="System.Exception"></exception>
  public List<object> ConvertToNative(List<Base> values);

  /// <summary>
  /// Converts a given speckle objects as a generic native object.
  /// This should assume <see cref="CanConvertToNativeDisplayable"/> has been called and returned True,
  /// or call it within this method's implementation to ensure non-displayable objects are gracefully handled.
  /// </summary>
  /// <remarks>
  /// This method should not try to convert an object to it's native representation (i.e Speckle Wall -> Wall),
  /// but rather use the 'displayValue' of that wall to create a geometrically correct representation of that object
  /// in the native application.
  /// An object may be able to be converted both with <see cref="ConvertToNative(Speckle.Core.Models.Base)"/> and <see cref="ConvertToNativeDisplayable"/>.
  /// In this case, deciding which to use is dependent on each connector developer.
  /// Preferably, <see cref="ConvertToNativeDisplayable"/> should be used as a fallback to the <see cref="ConvertToNative(Speckle.Core.Models.Base)"/> logic.
  /// </remarks>
  /// <param name="value">Speckle object to convert</param>
  /// <returns>The native object that resulted after converting the input <paramref name="value"/></returns>
  public object ConvertToNativeDisplayable(Base value);

  /// <summary>
  /// Checks if it can convert a Speckle object to a native one
  /// </summary>
  /// <param name="value">Speckle object to convert</param>
  /// <returns></returns>
  public bool CanConvertToNative(Base value);

  /// <summary>
  /// Checks to verify if a given object is: 1) displayable and  2) can be supported for conversion to the native application.
  /// An object is considered "displayable" if it has a 'displayValue' property (defined in its class or dynamically attached to it, detached or not).
  /// </summary>
  /// <remarks>
  /// An object may return "True" for both <see cref="CanConvertToNative"/> and <see cref="CanConvertToNativeDisplayable"/>
  /// In this case, deciding which to use is dependent on each connector developer.
  /// Preferably, <see cref="CanConvertToNativeDisplayable"/> should be used as a fallback to the <see cref="CanConvertToNative"/> logic.
  /// Objects found in the 'displayValue' property are assumed to be universally convertible by all converters and the viewer, but are not guaranteed to be so.
  /// </remarks>
  /// <param name="value">Speckle object to convert</param>
  /// <returns>True if the object is "displayable" and the converter supports native conversion of the given speckle object in particular.</returns>
  public bool CanConvertToNativeDisplayable(Base value);

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
