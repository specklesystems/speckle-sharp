using Speckle.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Kits
{

  public interface ISpeckleConverter
  {
    /// <summary>
    /// Converts a native object to a Speckle one
    /// </summary>
    /// <param name="object">Native object to convert</param>
    /// <returns></returns>
    public Base ToSpeckle(object @object);

    /// <summary>
    /// Checks if it can onvert a native object to a Speckle one
    /// </summary>
    /// <param name="object">Native object to convert</param>
    /// <returns></returns>
    public bool CanConvertToSpeckle(object @object);

    /// <summary>
    /// Converts a Speckle object to a native one
    /// </summary>
    /// <param name="object">Speckle object to convert</param>
    /// <returns></returns>
    public object ToNative(Base @object);

    /// <summary>
    /// Checks if it can convert a Speckle object to a native one
    /// </summary>
    /// <param name="object">Speckle object to convert</param>
    /// <returns></returns>
    public bool CanConvertToNative(Base @object);
  }

  public interface ISpeckleKit
  {
    /// <summary>
    /// Returns all the object types (the object model) provided by this kit.
    /// </summary>
    IEnumerable<Type> Types { get; }

    string Description { get; }
    string Name { get; }
    string Author { get; }
    string WebsiteOrEmail { get; }
  }
}
