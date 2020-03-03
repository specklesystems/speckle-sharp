using System;
using System.Collections.Generic;
using Speckle.Models;

namespace Speckle.Kits
{
  /// <summary>
  /// Base converter class.
  /// </summary>
  public abstract class Converter
  {
    public abstract IEnumerable<string> GetServicedApplications();

    public abstract void SetContextDocument(object @object);

    public abstract Base ToSpeckle(object @object);

    public abstract object ToNative(Base @object);
  }

  /// <summary>
  /// Converter that does nothing.
  /// </summary>
  public class BlankConverter : Converter
  {
    public override IEnumerable<string> GetServicedApplications() => new string[] { Applications.Other };

    public override void SetContextDocument(object @object)
    {
    }

    public override object ToNative(Base @object) => @object;

    public override Base ToSpeckle(object @object) => @object as Base;

  }
}
