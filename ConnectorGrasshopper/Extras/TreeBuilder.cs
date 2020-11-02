using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Speckle.Core.Kits;

namespace ConnectorGrasshopper.Extras
{
  public class TreeBuilder
  {
    private List<int> _path = new List<int>();
    private readonly ISpeckleConverter _converter;

    public TreeBuilder(ISpeckleConverter converter)
    {
      _converter = converter;
    }

    public DataTree<IGH_Goo> Build(object data)
    {
      _path = new List<int>();
      var tree = new DataTree<IGH_Goo>();
      if (data == null)
        return tree;

      RecurseNestedLists(data, tree);
      return tree;
    }

    private void RecurseNestedLists(object data, DataTree<IGH_Goo> tree)
    {
      if (data is List<object> list)
      {
        for (var i = 0; i < list.Count; i++)
        {
          var item = list[i];
          if (item is List<object>)
          {
            //add list index to path
            _path.Add(i);
            RecurseNestedLists(item, tree);

            //last sublist, remove its index from the path
            if (_path.Any())
              _path.RemoveAt(_path.Count - 1);
          }
          else
          {
            AddLeaf(item, tree);
          }
        }
      }
      else
      {
        AddLeaf(data, tree);
      }
    }

    private void AddLeaf(object data, DataTree<IGH_Goo> tree)
    {
      //paths must have at least one element
      if (!_path.Any())
        _path.Add(0);

      var path = new GH_Path(_path.ToArray());
      tree.Add(Utilities.TryConvertItemToNative(data, _converter), path);
    }
  }
}
