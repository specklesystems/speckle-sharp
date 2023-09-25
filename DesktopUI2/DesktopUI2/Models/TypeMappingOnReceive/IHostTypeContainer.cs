using System;
using System.Collections.Generic;
using System.Text;

namespace DesktopUI2.Models.TypeMappingOnReceive
{
  public interface IHostTypeContainer
  {
    public ICollection<ISingleHostType> GetAllTypes();
    public ICollection<ISingleHostType> GetTypesInCategory(string category);
  }
}
