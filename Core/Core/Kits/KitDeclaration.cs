#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Models;

namespace Speckle.Core.Kits;

/// <summary>
/// Needed so we can properly deserialize all the Base-derived objects from Core itself.
/// </summary>
public sealed class CoreKit : ISpeckleKit
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
}
