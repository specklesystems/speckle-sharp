using System.Collections.Generic;

namespace Speckle.Core.Models.GraphTraversal
{
  public interface ITraversalRule
  {
    internal IEnumerable<string> MembersToTraverse(Base b);
    internal bool DoesRuleHold(Base o);
  }
}
