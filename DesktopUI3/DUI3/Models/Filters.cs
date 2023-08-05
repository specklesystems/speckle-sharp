using System.Collections.Generic;
using DUI3.Utils;
using JetBrains.Annotations;

namespace DUI3.Models;

public static class KnownSendFilterTypeKeyNames
{
  public const string Everything = "everything";
  public const string Selection = "selection";
  public const string Layers = "layers";
  // TODO: add more as we go
}

/// <summary>
/// Defines what a publisher sends from the host application. 
/// </summary>
public abstract class SendFilter : DiscriminatedObject
{
  public string Name { get; set; }
  public string Summary { get; set; }
  public abstract List<string> GetObjectIds();

  public abstract bool CheckExpiry(string[] changedObjectIds);
}

/// <summary>
/// Simple direct user selection based filter. 
/// </summary>
public abstract class DirectSelectionSendFilter : SendFilter
{
  public List<string> SelectedObjectIds { get; set; }
}

/// <summary>
/// For filters like e.g. categories or views in revit, or layers in autocad.
/// </summary>
public abstract class ListSendFilter : SendFilter
{
  public List<string> Options { get; set; } = new List<string>();
  public List<string> SelectedOptions { get; set; } = new List<string>();
  public bool SingleSelection { get; set; } = false;
}

/// <summary>
/// For filters representing nested elements, such as layers in rhino. 
/// </summary>
public abstract class TreeListSendFilter : SendFilter
{
  private List<TreeListSendFilerNode> Nodes { get; set; }
}

[PublicAPI]
public class TreeListSendFilerNode
{
  public string Name { get; set; }
  public string Id { get; set; }
  public bool Selected { get; set; }
  public List<TreeListSendFilerNode> Children { get; set; } = new List<TreeListSendFilerNode>();
}

  
