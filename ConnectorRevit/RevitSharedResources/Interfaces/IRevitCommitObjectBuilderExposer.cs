using System;
using System.Collections.Generic;
using System.Text;

namespace RevitSharedResources.Interfaces
{
  public interface IRevitCommitObjectBuilderExposer
  {
    public IRevitCommitObjectBuilder commitObjectBuilder { get; }
  }
}
