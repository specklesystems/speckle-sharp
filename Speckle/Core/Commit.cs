using System;
using System.Collections.Generic;
using Speckle.Models;

namespace Speckle.Core
{
  public class Commit : Base
  {
    [DetachProperty]
    public List<Base> Objects { get; set; } = new List<Base>();

    [ExcludeHashing]
    public string Name { get; set; }

    [ExcludeHashing]
    public string Description { get; set; }

    [ExcludeHashing]
    public HashSet<string> Parents { get; set; } = new HashSet<string>();

    [ExcludeHashing]
    public User Author { get; set; }

    [ExcludeHashing]
    public string CreatedOn { get; } = DateTime.UtcNow.ToString("o");

    public Commit() { }
  }
}
