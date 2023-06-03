using System;
using System.Collections.Generic;
using System.Text;

namespace DesktopUI2.Models.TypeMappingOnReceive
{
  public interface ITypeMap
  {
    public IEnumerable<string> Categories { get; }
    public bool HasCategory(string category);
  }
}
