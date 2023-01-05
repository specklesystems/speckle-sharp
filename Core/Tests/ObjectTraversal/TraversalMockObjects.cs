using Speckle.Core.Models;

namespace TestsUnit.ObjectTraversal;

public class TraversalMock : Base
{
  public Base? Child { get; set; }

  public object ObjectChild { get; set; }
  
  public List<Base> ListChildren { get; set; } = new ();
  
  public List<List<Base>> NestedListChildren { get; set; } = new ();
  
  public Dictionary<string, Base> DictChildren { get; set; } = new ();
}
