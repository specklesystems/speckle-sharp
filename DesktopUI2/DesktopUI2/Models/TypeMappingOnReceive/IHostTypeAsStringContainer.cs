using System;
using System.Collections.Generic;
using System.Text;

namespace DesktopUI2.Models.TypeMappingOnReceive
{
  public interface IHostTypeAsStringContainer
  {
    public ICollection<string> GetAllTypes();
    public ICollection<string> GetTypesInCategory(string category);
  }
}
