using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Core.Kits
{

  public interface ISpeckleConverter
  {
    string Description { get; }
    string Name { get; }
    string Author { get; }
    string WebsiteOrEmail { get; }

    public HashSet<Exception> ConversionErrors { get; }

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
    public void SetContextObjects(List<ApplicationPlaceholderObject> objects);

    /// <summary>
    /// Some converters need to know which objects have been converted before in order to update them (ie, Revit). Use this method to set them.
    /// </summary>
    /// <param name="objects"></param>
    public void SetPreviousContextObjects(List<ApplicationPlaceholderObject> objects);
  }
}
