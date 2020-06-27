using System;
using System.Collections.Generic;
using System.Linq;
using Speckle.Kits;
using Speckle.Models;

namespace Speckle.Core
{
  /// <summary>
  /// Needed so we can properly deserialize all the Base-derived objects from Core itself.
  /// </summary>
  public class CoreKit : ISpeckleKit
  {
    public IEnumerable<Type> Types => GetType().Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(Base)));

    public IEnumerable<Type> Converters => GetType().Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(Converter)));

    public string Description => "Base Speckle models for revisions, streams, etc.";

    public string Name => nameof(CoreKit);

    public string Author => "Dimitrie";

    public string WebsiteOrEmail => "hello@speckle.works";

    public CoreKit() { }
  }
}
