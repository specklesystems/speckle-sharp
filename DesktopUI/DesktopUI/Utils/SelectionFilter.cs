using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;
using Speckle.Newtonsoft.Json;
using Speckle.DesktopUI.Streams.Dialogs.FilterViews;
using Stylet;

namespace Speckle.DesktopUI.Utils
{

  public interface ISelectionFilter
  {

    /// <summary>
    /// User friendly name displayed in the UI
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// MaterialDesignIcon use the demo app from the MaterialDesignInXamlToolkit to get the correct name
    /// </summary>
    string Icon { get; set; }


    /// <summary>
    /// Internal filter name 
    /// </summary>
    string Slug { get; set; }

    /// <summary>
    /// Used as the discriminator for deserialisation.
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Shoud return a succint summary of the filter: what does it contain inside?
    /// </summary>
    string Summary { get; }

    /// <summary>
    /// Should contain a generic description of the filter and how it works.
    /// </summary>
    string Description { get; set; }

    /// <summary>
    /// Holds the values that the user selected from the filter. Not the actual objects.
    /// </summary>    
    List<string> Selection { get; set; }
  }

  public class AllSelectionFilter : ISelectionFilter
  {
    public string Type => typeof(ListSelectionFilter).ToString();
    public string Name { get; set; }
    public string Slug { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }
    public List<string> Selection { get; set; } = new List<string>();
    public string Summary
    {
      get
      {
        return "Everything";
      }
    }
  }

  public class ListSelectionFilter : ISelectionFilter
  {
    public string Type => typeof(ListSelectionFilter).ToString();

    public string Name { get; set; }
    public string Slug { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }

    public List<string> Values { get; set; }
    public List<string> Selection { get; set; } = new List<string>();

    public string Summary
    {
      get
      {
        if (Selection.Count != 0)
        {
          return string.Join(", ", Selection);
        }
        else
        {
          return "Not set.";
        }
      }
    }
  }

  public class PropertySelectionFilter : ISelectionFilter
  {
    public string Type => typeof(PropertySelectionFilter).ToString();

    public string Name { get; set; }

    public string Slug { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }

    public List<string> Selection { get; set; } = new List<string>();

    public List<string> Values { get; set; }
    public List<string> Operators { get; set; }
    public string PropertyName { get; set; }
    public string PropertyValue { get; set; }
    public string PropertyOperator { get; set; }
    public bool HasCustomProperty { get; set; }

    public string Summary
    {
      get
      {
        return $"{PropertyName} {PropertyOperator} {PropertyValue}";
      }
    }
  }

  public class FilterTab : PropertyChangedBase
  {
    public string Name => Filter.Name;
    public string Slug => Filter.Slug;

    public ISelectionFilter Filter { get; }

    public object FilterView { get; private set; }

    private BindableCollection<string> _listItems = new BindableCollection<string>();

    public BindableCollection<string> ListItems
    {
      get => _listItems;
      set => SetAndNotify(ref _listItems, value);
    }

    private BindableCollection<string> _selectedListItems = new BindableCollection<string>();

    public BindableCollection<string> SelectedListItems
    {
      get => _selectedListItems;
      set
      {
        SetAndNotify(ref _selectedListItems, value);
        NotifyOfPropertyChange("Summary");
      }
    }

    public string Summary
    {
      get
      {
        return string.Join(", ", SelectedListItems.ToArray());
      }
    }


    public FilterTab(ISelectionFilter filter)
    {
      Filter = filter;

      switch (filter)
      {
        case PropertySelectionFilter f:
          FilterView = new ParameterFilterView();
          break;
        case ListSelectionFilter f:
          FilterView = new ListFilterView();
          _valuesList = SearchResults = new BindableCollection<string>(f.Values);
          break;
        case AllSelectionFilter f:
          FilterView = new AllFilterView();
          break;
      }
    }

    private string _searchQuery;
    public string SearchQuery
    {
      get => _searchQuery;
      set
      {
        SetAndNotify(ref _searchQuery, value);
        RestoreSelectedItems();
        SearchResults = new BindableCollection<string>(_valuesList.Where(v => v.ToLower().Contains(SearchQuery.ToLower())).ToList());
        NotifyOfPropertyChange(nameof(SearchResults));

      }
    }

    // searching will change data source and remove selected items in the ListBox, 
    // restore them as the query is cleared
    public void RestoreSelectedItems()
    {
      foreach (var item in SelectedListItems)
      {
        if (!ListItems.Contains(item))
          ListItems.Add(item);
      }
    }

    public BindableCollection<string> SearchResults { get; set; } = new BindableCollection<string>();
    private BindableCollection<string> _valuesList { get; }

  }

  public class SelectionFilterConverter : JsonConverter
  {
    public override bool CanConvert(Type objectType)
    {
      return objectType == typeof(ISelectionFilter);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
      var filter = serializer.Deserialize<ListSelectionFilter>(reader) ?? (ISelectionFilter)serializer.Deserialize<PropertySelectionFilter>(reader);

      return filter;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
      serializer.Serialize(writer, value);
    }
  }

  // Custom behavior for a list box to allow a two way binding of the SelectedItems property
  // we use it to set the previous value of a filter when it's edited
  // adapted from: https://tyrrrz.me/blog/wpf-listbox-selecteditems-twoway-binding
  public class ListFilterSelectionBehavior : ListBoxSelectionBehavior<string>
  {
  }
  public class ListBoxSelectionBehavior<T> : Behavior<ListBox>
  {
    public static readonly DependencyProperty SelectedItemsProperty =
        DependencyProperty.Register(nameof(SelectedItems), typeof(IList),
            typeof(ListBoxSelectionBehavior<T>),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedItemsChanged));

    private static void OnSelectedItemsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
      var behavior = (ListBoxSelectionBehavior<T>)sender;
      if (behavior._modelHandled) return;

      if (behavior.AssociatedObject == null)
        return;

      behavior._modelHandled = true;
      behavior.SelectItems();
      behavior._modelHandled = false;
    }

    private bool _viewHandled;
    private bool _modelHandled;

    public BindableCollection<T> SelectedItems
    {
      get => (BindableCollection<T>)GetValue(SelectedItemsProperty);
      set => SetValue(SelectedItemsProperty, value);
    }

    // Propagate selected items from model to view
    private void SelectItems()
    {
      _viewHandled = true;
      AssociatedObject.SelectedItems.Clear();
      if (SelectedItems != null)
      {
        foreach (var item in SelectedItems)
          AssociatedObject.SelectedItems.Add(item);
      }
      _viewHandled = false;
    }

    // Propagate selected items from view to model
    private void OnListBoxSelectionChanged(object sender, SelectionChangedEventArgs args)
    {
      if (_viewHandled) return;
      if (AssociatedObject.Items.SourceCollection == null) return;

      SelectedItems = new BindableCollection<T>(AssociatedObject.SelectedItems.OfType<T>());
    }

    // Re-select items when the set of items changes
    private void OnListBoxItemsChanged(object sender, NotifyCollectionChangedEventArgs args)
    {
      if (_viewHandled) return;
      if (AssociatedObject.Items.SourceCollection == null) return;

      SelectItems();
    }

    protected override void OnAttached()
    {
      base.OnAttached();

      AssociatedObject.SelectionChanged += OnListBoxSelectionChanged;
      ((INotifyCollectionChanged)AssociatedObject.Items).CollectionChanged += OnListBoxItemsChanged;
    }

    /// <inheritdoc />
    protected override void OnDetaching()
    {
      base.OnDetaching();

      if (AssociatedObject != null)
      {
        AssociatedObject.SelectionChanged -= OnListBoxSelectionChanged;
        ((INotifyCollectionChanged)AssociatedObject.Items).CollectionChanged -= OnListBoxItemsChanged;
      }
    }
  }
}
