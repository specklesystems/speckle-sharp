using Rhino;
using Rhino.DocObjects;
using System.Collections.Generic;

namespace ConnectorRhinoWebUI.Utils;

public class BlockHierarchyPaths
{
  public static List<List<string>> GetObjectHierarchyPaths(RhinoObject rhinoObject, RhinoDoc doc)
  {
    List<List<string>> paths = new();
    // Check if the RhinoObject is part of any instance definition
    foreach (var def in doc.InstanceDefinitions)
    {
      if (def == null || def.IsDeleted)
      {
        continue;
      }

      if (IsObjectInDefinition(rhinoObject, def))
      {
        // If the object is in this definition, start building the path from here
        List<string> currentPath = new() { def.Name };
        ExploreChildren(def, currentPath, paths, rhinoObject);
      }
    }
    return paths;
  }

  private static bool IsObjectInDefinition(RhinoObject obj, InstanceDefinition def)
  {
    var objs = def.GetObjects();
    foreach (var defObj in objs)
    {
      if (defObj.Id == obj.Id)
      {
        return true;
      }
      else if (defObj is InstanceObject instanceObj)
      {
        if (IsObjectInDefinition(obj, instanceObj.InstanceDefinition))
        {
          return true;
        }
      }
    }
    return false;
  }

  private static void ExploreChildren(
    InstanceDefinition parentDef,
    List<string> currentPath,
    List<List<string>> paths,
    RhinoObject targetObject
  )
  {
    var objs = parentDef.GetObjects();
    bool foundTarget = false;
    foreach (var obj in objs)
    {
      if (obj is InstanceObject instanceObj)
      {
        var childDef = instanceObj.InstanceDefinition;
        if (childDef != null && !childDef.IsDeleted)
        {
          // Check if the target object is in the child definition
          if (IsObjectInDefinition(targetObject, childDef))
          {
            foundTarget = true;
            List<string> newPath = new List<string>(currentPath) { childDef.Name };
            ExploreChildren(childDef, newPath, paths, targetObject);
          }
        }
      }
    }

    if (!foundTarget && currentPath.Count > 1)
    {
      // If the target object is part of this path but we didn't find it in children,
      // it means this path is valid up to this point and should be added.
      paths.Add(currentPath);
    }
  }

  public static List<List<string>> GetBlockHierarchyPaths(RhinoDoc doc)
  {
    List<List<string>> paths = new();
    var instanceDefinitions = doc.InstanceDefinitions;

    foreach (var def in instanceDefinitions)
    {
      if (def == null || def.IsDeleted)
      {
        continue;
      }
      // Start a new path with the current parent block
      List<string> currentPath = new() { def.Name };
      ExploreChildren(def, currentPath, paths);
    }

    return paths;
  }

  private static void ExploreChildren(InstanceDefinition parentDef, List<string> currentPath, List<List<string>> paths)
  {
    var objs = parentDef.GetObjects();
    bool hasChildren = false;
    foreach (var obj in objs)
    {
      if (obj is InstanceObject instanceObj)
      {
        var childDef = instanceObj.InstanceDefinition;
        if (childDef != null && !childDef.IsDeleted)
        {
          // Found a child, mark and extend the path
          hasChildren = true;
          List<string> newPath = new(currentPath) { childDef.Name };
          ExploreChildren(childDef, newPath, paths);
        }
      }
    }

    // If the block has no children, add the current path to the paths list
    if (!hasChildren)
    {
      paths.Add(currentPath);
    }
  }
}
