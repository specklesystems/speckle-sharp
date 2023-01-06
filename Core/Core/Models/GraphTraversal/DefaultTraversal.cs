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
    
    // Traversal function mvp based on Rhino connector's traversal
    public static GraphTraversal CreateTraverseFunc(ISpeckleConverter converter)
    {
      var convertableRule = TraversalRule.NewTraveralRule()
        .When(converter.CanConvertToNative)
        .When(HasDisplayValue)
        .ContinueTraversing(Except(
          Concat(Members(DynamicBaseMemberType.Dynamic), ElementsAliases), 
          displayValueAliases)
        );

      var defaultRule = TraversalRule.NewTraveralRule()
        .When(_ => true)
        .ContinueTraversing(Members(DynamicBaseMemberType.All));

      return new GraphTraversal(convertableRule, defaultRule);
    }
    
    
    
    #region Helper Functions

    private static readonly string[] elementsAliases = { "elements", "@elements" };
    public static IEnumerable<string> ElementsAliases(Base _) => elementsAliases;

    private static readonly string[] displayValueAliases = { "displayValue", "@displayValue" };
    public static IEnumerable<string> DisplayValueAliases(Base _) => displayValueAliases;
    public static IEnumerable<string> None(Base x) => Enumerable.Empty<string>();
    public static SelectMembers Members(DynamicBaseMemberType includeMembers) => x => x.GetMembers(includeMembers).Keys;
    public static SelectMembers Concat(params SelectMembers[] selectProps) => x => selectProps.SelectMany(i => i.Invoke(x));
    public static SelectMembers Except(SelectMembers selectProps, IEnumerable<string> excludeProps) => x => selectProps.Invoke(x).Except(excludeProps);
    public static bool HasElements(Base x) => x.GetMembers(DynamicBaseMemberType.Instance).Keys.Any(member => elementsAliases.Contains(member));
    public static bool HasDisplayValue(Base x) => x.GetMembers(DynamicBaseMemberType.Instance).Keys.Any(member => displayValueAliases.Contains(member));
    
    #endregion
    
    
    
    // public static GraphTraversal CreateTraverseFunc_Old(ISpeckleConverter converter)
    // {
    //   //Define traversal Rules
    //   var elementsRule = TraversalRule.NewTraveralRule()
    //     .When(HasElements)
    //     .ContinueTraversing(x => converter.CanConvertToNative(x)
    //       ? elementsAliases
    //       : elementsAliases.Concat(displayValueAliases)
    //       );
    //
    //   var convertableRule = TraversalRule.NewTraveralRule()
    //     .When(converter.CanConvertToNative)
    //     .ContinueTraversing(Concat(Members(DynamicBaseMemberType.Dynamic), ElementsAliases));
    //
    //   var defaultRule = TraversalRule.NewTraveralRule()
    //     .When(_ => true)
    //     .ContinueTraversing(Members(DynamicBaseMemberType.All));
    //
    //   return new GraphTraversal(elementsRule, convertableRule, defaultRule);
    // }
    
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
