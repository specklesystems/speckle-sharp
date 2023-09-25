using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;

namespace DesktopUI2.Models.TypeMappingOnReceive
{
  public interface ITypeMap
  {
    public IEnumerable<string> Categories { get; }
    public IEnumerable<ISingleValueToMap> GetValuesToMapOfCategory(string category);
  }
}
