#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using Speckle.Core.Logging;
using Speckle.Core.Models.Extensions;

namespace Speckle.Core.Models;

public abstract class CommitObjectBuilder
{
  protected const string Root = "__Root";

  
  /// <summary>app id -> base </summary>
  protected readonly IDictionary<string, Base> converted;
  
  /// <summary>Base -> Tuple{Parent App Id, propName} ordered by priority</summary>
  protected readonly IDictionary<Base, IList<(string? parentAppId, string propName)>> parentInfos;
  
  protected CommitObjectBuilder()
  {
    converted = new Dictionary<string, Base>();
    parentInfos = new Dictionary<Base, IList<(string?,string)>>();
  }
  
  protected void AddRelationship(Base conversionResult, params (string? parentAppId, string propName)[] parentInfo)
  {
    converted.Add(conversionResult.applicationId, conversionResult);
    parentInfos.Add(conversionResult, parentInfo );
  }
  
  public void BuildCommitObject(Base rootCommitObject)
  {
    foreach (Base c in converted.Values)
    {
      try
      {
        AddToRoot(c, rootCommitObject);
      }
      catch(Exception ex)
      {
        // This should never happen, we should be ensuring that at least one of the parents is valid.
        SpeckleLog.Logger.Fatal(ex, "Failed to add object {speckleType} to commit object", c?.GetType());
      }
    }
  }
  
  private void AddToRoot(Base current, Base rootCommitObject)
  {
    var parents = parentInfos[current];
    foreach ((string? parentAppId, string propName) in parents)
    {
      if(parentAppId is null) continue;
          
      Base? parent;
      if (parentAppId == Root) parent = rootCommitObject;
      else converted.TryGetValue(parentAppId, out parent);
          
      if(parent is null)
        continue;
          
      try
      {
        if (parent.GetDetachedProp(propName) is not IList elements)
        {
          elements = new List<Base>();
          parent.SetDetachedProp(propName, elements);
        }
        elements.Add(current);
        return;
      }
      catch(Exception ex)
      {
        // A parent was found, but it was invalid (Likely because of a type mismatch on a `elements` property)
        SpeckleLog.Logger.Warning(ex, "Failed to add object {speckleType} to a converted parent.", current?.GetType());
      }
    }
    throw new InvalidOperationException($"Could not find a valid parent for object of type {current?.GetType()}. Checked {parents.Count} potential parent, and non were converted!");
  }
}
