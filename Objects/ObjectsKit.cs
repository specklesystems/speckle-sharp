using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Speckle.Objects
{
  public class ObjectsKit : ISpeckleKit
  {

    public string Description => "Default Speckle Kit";
    public string Name => "Objects";
    public string Author => "Speckle";
    public string WebsiteOrEmail => "https://speckle.systems";

    public IEnumerable<Type> Types => Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(Base)) && !t.IsAbstract);
  }
}
