using System;
using System.Collections.Generic;
using System.Text;

namespace RevitSharedResources.Interfaces
{
  /// <summary>
  /// Provides access to an entity of type <typeparamref name="TProvided"/>
  /// </summary>
  public interface IEntityProvider<TProvided>
  {
    TProvided Entity { get; set; }
  }
}
