using System.Collections.Generic;

namespace DesktopUI2.Models.TypeMappingOnReceive;

public interface ITypeMap
{
  public IEnumerable<string> Categories { get; }
  public IEnumerable<ISingleValueToMap> GetValuesToMapOfCategory(string category);
}
