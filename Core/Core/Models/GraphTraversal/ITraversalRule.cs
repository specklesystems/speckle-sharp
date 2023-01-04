using System.Collections.Generic;
using System.Linq;

#nullable enable
namespace Speckle.Core.Models.GraphTraversal
{
  public interface ITraversalRule
  {
    public IEnumerable<string> MembersToTraverse(Base b);
    public bool DoesRuleHold(Base o);
  }
  
  /// <summary>
  /// The traverse none rule to default, that always holds true
  /// </summary>
  public class DefaultRule : ITraversalRule
  {
    private static DefaultRule? instance;
    public static DefaultRule Instance => instance ??= new DefaultRule();

    private DefaultRule() { }
    public IEnumerable<string> MembersToTraverse(Base b) => Enumerable.Empty<string>();
    public bool DoesRuleHold(Base o) => true;
  }
}
