using Speckle.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Kits
{
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
