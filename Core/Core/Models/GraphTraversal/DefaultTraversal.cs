using System;
using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Kits;

#nullable enable
namespace Speckle.Core.Models.GraphTraversal
{
  public class DefaultTraversal
  {

    ISpeckleConverter converter;

    private static readonly string[] elementsAliases = { "elements", "@elements" };
    private static readonly string[] displayValueAliases = { "displayValue", "@displayValue" };
    public static IEnumerable<string> ElementsAliases(Base _) => elementsAliases;
    public static IEnumerable<string> DisplayValueAliases(Base _) => displayValueAliases;
    public static IEnumerable<string> None(Base x) => Enumerable.Empty<string>();
    public static SelectMembers Members(DynamicBaseMemberType includeMembers) => x => x.GetMembers(includeMembers).Keys;
    public static SelectMembers Concat(params SelectMembers[] selectProps) => x => selectProps.SelectMany(i => i.Invoke(x));

    public static GraphTraversal CreateTraverseFunc(ISpeckleConverter converter)
    {
      //Define traversal Rules
      var convertableRule = TraversalRule.NewTraveralRule()
        .When(x=> x.GetMembers(DynamicBaseMemberType.Instance).Keys.Any(member => elementsAliases.Contains(member)))
        .ContinueTraversing(x => converter.CanConvertToNative(x)
          ? elementsAliases.Concat(displayValueAliases)
          : elementsAliases);

      var defaultRule = TraversalRule.NewTraveralRule()
        .When(_ => true)
        .ContinueTraversing(Members(DynamicBaseMemberType.All));

      return new GraphTraversal(convertableRule, defaultRule);
    }
    
    
    
    internal void TraverseExample()
    {
      //Receive Objects
      Base commitObject = new Base(); // await Operations.Receive(...

      var traverseFunction = CreateTraverseFunc(converter);
      
      //Traverse
      var objectsToTraverse = traverseFunction
        .Traverse(commitObject)
        .Where(c => converter.CanConvertToNative(c.current));
      
      foreach (var c in objectsToTraverse)
      {
        Base current = c.current;

        string layer = ""; // some recursive call up the parental structure
                
        object nativeObject = converter.ConvertToNative(current);
        
      }
    }
  }
}
