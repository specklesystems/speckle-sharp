using System;
using System.Collections.Generic;
using System.Text;

namespace RevitSharedResources.Interfaces
{
  public interface IEntityProvider<TProvided>
  {
    TProvided Entity { get; set; }
  }
}
