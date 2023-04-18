using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using DesktopUI2.Views.Filters;
using Speckle.Newtonsoft.Json;

namespace DesktopUI2.Models.Filters;

public class TreeSelectionFilter : ISelectionFilter
{
  public TreeSelectionFilter()
  {
    SelectedItems.CollectionChanged += Items_CollectionChanged;
  }

  // Doesn't Start with the Root Node, but all list of all first tier children of the theoretical Root Node
  public List<TreeNode> Values { get; set; }

  public ObservableCollection<TreeNode> SelectedItems { get; set; } = new();

  public string SelectionMode { get; set; } = "Multiple, Toggle";

  /// <summary>
  /// Used as the discriminator for deserialization.
  /// </summary>
  public string Type => typeof(TreeSelectionFilter).ToString();

  /// <summary>
  /// User friendly name displayed in the UI
  /// </summary>
  public string Name { get; set; }

  /// <summary>
  /// Internal filter name
  /// </summary>
  public string Slug { get; set; }

  /// <summary>
  /// MaterialDesignIcon use the demo app from the MaterialDesignInXamlToolkit to get the correct name
  /// </summary>
  public string Icon { get; set; }

  /// <summary>
  /// Should contain a generic description of the filter and how it works.
  /// </summary>
  public string Description { get; set; }

  /// <summary>
  /// Holds the values that the user selected from the filter. Not the actual objects.
  /// </summary>
  public List<string> Selection { get; set; } = new();

  Type ISelectionFilter.ViewType { get; } = typeof(TreeFilterView);

  /// <summary>
  /// Should return a succinct summary of the filter: what does it contain inside?
  /// </summary>
  public string Summary =>
    SelectedItems.Count != 0 ? string.Join(", ", SelectedItems.Select(item => item.DisplayName)) : "nothing";

  private void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
  {
    Selection.Clear();
    if (SelectedItems != null)
      Selection.AddRange(SelectedItems.Select(item => item.ToString()).ToList());
  }
}

// Vanilla Tree Hierarchical Object type. Use-cases may extend with dynamic properties.
public class TreeNode : DynamicObject
{
  [JsonProperty("DisplayName")]
  public string DisplayName { get; set; }

  [JsonProperty("Elements")]
  public List<TreeNode> Elements { get; set; } = new();

  // For use with LINQ queries and presentation
  [JsonProperty("IsSelected")]
  public bool IsSelected { get; set; } = false;

  // For applications that record the pointer as a Guid
  [JsonProperty("Guid")]
  public Guid Guid { get; set; }

  // For applications that record the pointer as a Guid
  [JsonProperty("Reference")]
  public string Reference { get; set; }

  // For applications that record the pointer as successive indexes
  [JsonProperty("Indices")]
  public int[] Indices { get; set; } = { };

  // For applications that record the pointer as a hash
  [JsonProperty("Hash")]
  public object Hash { get; set; }

  [JsonProperty("IndexWith")]
  public string IndexWith { get; set; } = nameof(Guid);

  [JsonProperty("IsEnabled")]
  public bool IsEnabled { get; set; } = true;

  public override string ToString()
  {
    string value;

    switch (IndexWith)
    {
      case nameof(Guid):
        value = $"{Guid}";
        break;
      case nameof(Indices):
        value = string.Join(",", Indices);
        break;
      case nameof(DisplayName):
        value = DisplayName;
        break;
      case nameof(Hash):
        value = $"{Hash}";
        break;
      default:
        PropertyInfo prop = typeof(TreeNode).GetProperty(IndexWith);
        value = prop != null ? prop.GetValue(this, null).ToString() : string.Empty;
        break;
    }

    return value;
  }

  public List<TreeNode> Flatten()
  {
    var result = new List<TreeNode> { this };
    foreach (var child in Elements.Where(child => child != null))
      result.AddRange(child.Flatten());
    return result;
  }

  #region Dynamic Property Handling

  private readonly Dictionary<string, object> _members = new();

  public override bool TryGetMember(GetMemberBinder binder, out object result)
  {
    string name = binder.Name.ToLower();
    return _members.TryGetValue(name, out result);
  }

  public override bool TrySetMember(SetMemberBinder binder, object value)
  {
    _members[binder.Name.ToLower()] = value;

    return true;
  }

  public override IEnumerable<string> GetDynamicMemberNames()
  {
    return _members.Keys;
  }

  #endregion
}
