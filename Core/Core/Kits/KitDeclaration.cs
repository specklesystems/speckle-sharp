using System;
using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Models;

namespace Speckle.Core.Kits;

/// <summary>
/// Needed so we can properly deserialize all the Base-derived objects from Core itself.
/// </summary>
public class CoreKit : ISpeckleKit
{
  public IEnumerable<Type> Types => GetType().Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(Base)));

  public string Description => "Base Speckle models for revisions, streams, etc.";

  public string Name => nameof(CoreKit);

  public string Author => "Dimitrie";

  public string WebsiteOrEmail => "hello@speckle.systems";

  public IEnumerable<string> Converters => new List<string>();

  public ISpeckleConverter LoadConverter(string app)
  {
    return null;
  }

  public Base ToSpeckle(object @object)
  {
    throw new NotImplementedException();
  }

  public bool CanConvertToSpeckle(object @object)
  {
    throw new NotImplementedException();
  }

  public object ToNative(Base @object)
  {
    throw new NotImplementedException();
  }

  public bool CanConvertToNative(Base @object)
  {
    throw new NotImplementedException();
  }

  public IEnumerable<string> GetServicedApplications()
  {
    throw new NotImplementedException();
  }

  public void SetContextDocument(object @object)
  {
    throw new NotImplementedException();
  }

  public bool TryLoadConverter(string app, out ISpeckleConverter speckleConverter)
  {
    throw new NotImplementedException();
  }
}
