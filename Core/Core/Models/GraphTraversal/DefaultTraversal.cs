using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Kits;

#nullable enable
namespace Speckle.Core.Models.GraphTraversal
{
  public class DefaultTraversal
  {

    ISpeckleConverter converter;

    private static readonly string[] onlyElementsArr = { "elements", "@elements" };
    public static IEnumerable<string> OnlyElements(Base _) => onlyElementsArr;
    public static IEnumerable<string> AllMembers(Base x) => x.GetMembers().Keys;
    public static IEnumerable<string> DynamicOnly(Base x) => x.GetMembers(DynamicBaseMemberType.Dynamic).Keys;
    public static IEnumerable<string> None(Base x) => Enumerable.Empty<string>();
    
    public static GraphTraversal CreateTraverseFunc(ISpeckleConverter converter)
    {
      //Define traversal Rules
      var convertableRule = TraversalRule.NewTraveralRule()
        .When(converter.CanConvertToNative)
        .ContinueTraversing(OnlyElements);

      var defaultRule = TraversalRule.NewTraveralRule()
        .When(x => true)
        .ContinueTraversing(AllMembers);
      
      return new GraphTraversal(convertableRule, defaultRule);
    }
    
    
    
    internal void TraverseExample()
    {
      //Receive Objects
      Base commitObject = new Base(); // await Operations.Receive(...

      var traverseFunction = CreateTraverseFunc(converter);
      
      //Traverse
      var objectstoTraverse = traverseFunction
        .Traverse(commitObject)
        .Where(c => converter.CanConvertToNative(c.current));
      
      foreach (var c in objectstoTraverse)
      {
        Base current = c.current;

        string layer = ""; // some recursive call up the parental structure
                
        object nativeObject = converter.ConvertToNative(current);
        
      }
    }
  }
}
