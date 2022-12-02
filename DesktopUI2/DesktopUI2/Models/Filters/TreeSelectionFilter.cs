using DesktopUI2.Views.Filters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using Autodesk.Navisworks.Gui;
using DynamicData;

namespace DesktopUI2.Models.Filters
{
    // Vanilla Tree Hierarchical Object type. Use-cases may extend with dynamic properties.
    public class ObjectHierarchy : DynamicObject
    {
        public string DisplayName { get; set; }
        public List<ObjectHierarchy> Elements { get; set; } = new List<ObjectHierarchy>();

        // For use with LINQ queries and presentation
        public bool IsSelected { get; set; } = false;

        // For applications that record the pointer as a Guid
        public Guid Guid { get; set; }

        // For applications that record the pointer as a Guid
        public String Reference { get; set; }

        // For applications that record the pointer as successive indexes
        public int[] Indices { get; set; }

        // For applications that record the pointer as a hash
        public object Hash { get; set; }

        public string IndexWith { get; set; } = nameof(Guid);


        #region Dynamic Property Handling

        private readonly Dictionary<string, object> _members = new Dictionary<string, object>();

        public override bool TryGetMember(
            GetMemberBinder binder, out object result)
        {
            string name = binder.Name.ToLower();
            return _members.TryGetValue(name, out result);
        }

        public override bool TrySetMember(
            SetMemberBinder binder, object value)
        {
            _members[binder.Name.ToLower()] = value;

            return true;
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return this._members.Keys;
        }

        #endregion

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
                    PropertyInfo prop = typeof(ObjectHierarchy).GetProperty(IndexWith);
                    value = (prop != null) ? prop.GetValue(this, null).ToString() : string.Empty;
                    break;
            }

            return value;
        }
    }

    public class SelectedItems : ObservableCollection<ObjectHierarchy>
    {
        public SelectedItems() : base()
        {
        }
    }


    public class TreeSelectionFilter : ISelectionFilter
    {
        public string Type => typeof(TreeSelectionFilter).ToString();
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }

        // Doesn't Start with the Root Node, but all list of all children of the theoretical Root Node
        public List<ObjectHierarchy> Values { get; set; }
        public List<string> Selection { get; set; } = new List<string>();

        public SelectedItems SelectedItems { get; set; } = new SelectedItems();
        public Type ViewType { get; } = typeof(TreeFilterView);

        public TreeSelectionFilter()
        {
            SelectedItems.CollectionChanged += Items_CollectionChanged;
        }

        private void Items_CollectionChanged(object sender,
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Selection.Clear();
            Selection.AddRange(SelectedItems.Select(item => item.ToString()).ToList());
        }

        public string Summary
        {
            get
            {
                if (SelectedItems.Count != 0)
                {
                    return string.Join(", ", SelectedItems.Select(item=>item.DisplayName));
                }
                else
                {
                    return "nothing";
                }
            }
        }
    }
}