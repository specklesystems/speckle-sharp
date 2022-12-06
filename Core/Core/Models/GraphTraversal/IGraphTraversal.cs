using System.Collections.Generic;

namespace Speckle.Core.Models.GraphTraversal
{
  public interface IGraphTraversal
  {
    IEnumerable<Base> Traverse(Base root);
  }
}
