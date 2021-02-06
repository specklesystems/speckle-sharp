using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Utilities = ConnectorGrasshopper.Extras.Utilities;

namespace ConnectorGrasshopper.Objects
{
  public class 
    CreateSpeckleObjectByKeyValue : SelectKitComponentBase
  {
    public CreateSpeckleObjectByKeyValue() : base("Create object by key/value", "K/V",
      "Create an Speckle object by key/value pairs.\nALPHA: Currently supports passing items as lists of keys (1 branch = 1 object), and values as a list of items or as a tree where each branch will be a list of values for each key.", ComponentCategories.PRIMARY_RIBBON, ComponentCategories.OBJECTS)
    {
    }

    public override Guid ComponentGuid => new Guid("75B07031-0180-4A1F-9AC9-3AAA81E11E05");

    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override bool Obsolete => true;

    protected override Bitmap Icon => Properties.Resources.CreateSpeckleObjectByKeyValue;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddTextParameter("Keys", "K", "List of keys", GH_ParamAccess.list);
      pManager.AddGenericParameter("Values", "V", "List of values", GH_ParamAccess.tree);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleBaseParam("Speckle Object", "O", "Speckle object", GH_ParamAccess.item));
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      // Initialize local variables
      var valueTree = new GH_Structure<IGH_Goo>();
      var keys = new List<string>();

      // Get data from inputs
      if (!DA.GetDataList(0, keys)) return;
      if (!DA.GetDataTree(1, out valueTree)) return;

      // Create a path from the current iteration
      var searchPath = new GH_Path(DA.Iteration);

      // Grab the corresponding subtree from the value input tree.
      var subTree = Utilities.GetSubTree(valueTree, searchPath);
      Base speckleObj = new Base();
      // Find the list or subtree belonging to that path
      if (valueTree.PathExists(searchPath) || valueTree.Paths.Count == 1)
      {
        var list = valueTree.Paths.Count == 1 ? valueTree.Branches[0] : valueTree.get_Branch(searchPath);
        // We got a list of values
        var ind = 0;
        keys.ForEach(key =>
        {
          if (ind < list.Count)
            speckleObj[key] = Utilities.TryConvertItemToSpeckle(list[ind], Converter);
          ind++;
        });
      }
      else
      {
        // We got a tree of values

        // Create the speckle object with the specified keys
        var index = 0;
        keys.ForEach(key =>
        {
          var itemPath = new GH_Path(index);
          //TODO: Grab conversion methods and implement branch handling.
          var branch = subTree.get_Branch(itemPath);
          if (branch != null)
          {
            List<object> objs = new List<object>();
            foreach (var goo in branch)
            {
              objs.Add(Utilities.TryConvertItemToSpeckle(goo, Converter));
            }

            if (objs.Count > 0)
              speckleObj[key] = objs;
          }

          index++;
        });
      }

      // Set output
      DA.SetData(0, new GH_SpeckleBase { Value = speckleObj });
    }

    protected override void BeforeSolveInstance()
    {
      Converter.SetContextDocument(Rhino.RhinoDoc.ActiveDoc);
      Tracker.TrackPageview("objects", "create", "keyvalue");
      base.BeforeSolveInstance();
    }
  }
}
