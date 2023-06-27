using System;
using System.Collections.Generic;
using System.Text;

namespace DesktopUI2.Models.TypeMappingOnReceive
{
  public interface ISingleHostType
  {
    string HostTypeName { get; }
    string HostTypeDisplayName { get; }
  }
}
