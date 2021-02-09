using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Utilities = ConnectorGrasshopper.Extras.Utilities;

namespace ConnectorGrasshopper.Objects
{
  public class ExtendSpeckleObject : SelectKitComponentBase
  {
    public override Guid ComponentGuid => new Guid("F208013C-AF46-4643-AF89-62B1A2435493");

    protected override Bitmap Icon => Properties.Resources.ExtendSpeckleObject;

    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override bool Obsolete => true;

    public ExtendSpeckleObject() : base("Extend Speckle Object", "ESO", "Extend a current object with key/value pairs.\nALPHA: Currently supports passing values as a list of items (one item per key) or as a tree where each branch will be a list of values for each key (one branch per key).", ComponentCategories.PRIMARY_RIBBON, ComponentCategories.OBJECTS)
    {
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleBaseParam("Speckle Object", "O", "Speckle object to deconstruct into it's properties.", GH_ParamAccess.item));
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleBaseParam("Speckle Object", "O", "Speckle object to deconstruct into it's properties.", GH_ParamAccess.item));
      pManager.AddTextParameter("Keys", "K", "List of keys", GH_ParamAccess.list);
      pManager.AddGenericParameter("Values", "V", "List of values", GH_ParamAccess.tree);
    }

    private List<string> lastSolutionKeys = null;

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      // Init local variables
      GH_SpeckleBase ghBase = null;
      var keys = new List<string>();

      // Grab data from input
      if (!DA.GetData(0, ref ghBase)) return;
      if (!DA.GetDataList(1, keys)) return;
      if (!DA.GetDataTree(2, out GH_Structure<IGH_Goo> valueTree)) return;

      // TODO: Handle data validation
      var b = ghBase.Value.ShallowCopy();
      CleanDeletedKeys(b, keys);
      // Search for the path coinciding with the current iteration.
      var path = new GH_Path(DA.Iteration);
      if (valueTree.PathExists(path))
      {
        var values = valueTree.get_Branch(path) as List<IGH_Goo>;
        // Input is a list of values. Assign them directly
        if (keys.Count != values?.Count)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Key and Value lists are not the same length.");
          return;
        }
        AssignToObject(b, keys, values);
      }
      else if (valueTree.Branches.Count == 1)
      {
        var values = valueTree.Branches[0];
        if (keys.Count != values.Count)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Key and Value lists are not the same length.");
          return;
        }
        // Input is just one list, so use it.
        AssignToObject(b, keys, values);
      }
      else
      {
        // Input is a tree, meaning it's values are either lists or trees.
        var subTree = Utilities.GetSubTree(valueTree, path);
        int index = 0;
        keys.ForEach(key =>
        {
          var subPath = new GH_Path(index);
          if (subTree.PathExists(subPath))
          {
            // Value is a list, convert and assign.
            var list = subTree.get_Branch(subPath) as List<IGH_Goo>;
            if (list?.Count > 0)
            {
              var converted = list.Select(goo => Utilities.TryConvertItemToSpeckle(goo, Converter)).ToList();
              b[key] = converted;
            }
          }
          else
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Cannot handle trees yet");
          }
          index++;
        });
      }

      lastSolutionKeys = keys;
      DA.SetData(0, new GH_SpeckleBase { Value = b });
    }

    private void CleanDeletedKeys(Base @base, List<string> keys)
    {
      lastSolutionKeys?.ForEach(key =>
      {
        var contains = keys.Contains(key);
        if (!contains)
        {
          // Key has been deleted
          @base[key] = null;
        }
      });
    }

    private void AssignToObject(Base b, List<string> keys, List<IGH_Goo> values)
    {
      var index = 0;
      keys.ForEach(key =>
      {
        if (b[key]!=null)
          AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Object {b.id} - Property {key} has been overwritten");
        b[key] = Utilities.TryConvertItemToSpeckle(values[index++], Converter);
      });
    }

    protected override void BeforeSolveInstance()
    {
      Converter.SetContextDocument(Rhino.RhinoDoc.ActiveDoc);
      Tracker.TrackPageview("objects", "extend");
      base.BeforeSolveInstance();
    }
  }
}
