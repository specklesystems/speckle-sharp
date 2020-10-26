using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ConnectorGrashopper.Extras;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Utilities = ConnectorGrashopper.Extras.Utilities;

namespace ConnectorGrashopper.Objects
{
  public class CreateSpeckleObjectByKeyValueAsync : GH_AsyncComponent
  {
    public ISpeckleConverter Converter;

    public ISpeckleKit Kit;

    public override Guid ComponentGuid
    {
      get => new Guid("C8D4DBEB-7CC5-45C0-AF5D-F374FA5DBFBB");
    }

    protected override System.Drawing.Bitmap Icon
    {
      get => null;
    }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    public CreateSpeckleObjectByKeyValueAsync() : base("Create Speckle Object by Key/Value Async", "K/V Async",
      "Creates a speckle object from key value pairs", "Speckle 2", "Object Management")
    {
      Kit = KitManager.GetDefaultKit();
      try
      {
        Converter = Kit.LoadConverter(Applications.Rhino);
        BaseWorker = new CreateSpeckleObjectByKeyValueWorker(Converter);
        Message = $"{Kit.Name} Kit";
      }
      catch
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No default kit found on this machine.");
      }
    }

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      Menu_AppendSeparator(menu);
      var menuItem = Menu_AppendItem(menu, "Select the converter you want to use:");
      menuItem.Enabled = false;
      var kits = KitManager.GetKitsWithConvertersForApp(Applications.Rhino);

      foreach (var kit in kits)
      {
        Menu_AppendItem(menu, $"{kit.Name} ({kit.Description})", (s, e) => { SetConverterFromKit(kit.Name); }, true, kit.Name == Kit.Name);
      }

      Menu_AppendSeparator(menu);
    }

    public void SetConverterFromKit(string kitName)
    {
      if (kitName == Kit.Name) return;

      Kit = KitManager.Kits.FirstOrDefault(k => k.Name == kitName);
      Converter = Kit.LoadConverter(Applications.Rhino);
      BaseWorker = new CreateSpeckleObjectByKeyValueWorker(Converter);
      Message = $"Using the {Kit.Name} Converter";
      ExpireSolution(true);
    }
    
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddTextParameter("Keys", "K", "List of keys", GH_ParamAccess.list);
      pManager.AddGenericParameter("Values", "V", "List of values", GH_ParamAccess.tree);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleBaseParam("Object", "O", "Speckle object", GH_ParamAccess.item));
    }
  }

  public class CreateSpeckleObjectByKeyValueWorker : WorkerInstance
  {
    private GH_Structure<IGH_Goo> valueTree = new GH_Structure<IGH_Goo>();
    private List<string> keys = new List<string>();
    private Base speckleObj;
    private int iteration;
    public ISpeckleConverter Converter;

    public CreateSpeckleObjectByKeyValueWorker(ISpeckleConverter converter) : base(null)
    {
      Converter = converter;
    }

    public override void DoWork(Action<string, double> ReportProgress, Action Done)
    {
      // 👉 Checking for cancellation!
      if (CancellationToken.IsCancellationRequested) return;


      // Do something here!

      // Create a path from the current iteration
      var searchPath = new GH_Path(iteration);

      // Grab the corresponding subtree from the value input tree.
      var subTree = GetSubTree(valueTree, searchPath);
      speckleObj = new Base();
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
          
          var branch = subTree.get_Branch(itemPath);
          if (branch != null)
          {
            var objs = new List<object>();
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
      // --> Report progress if necessary
      // ReportProgress(Id, percentage);


      // Set the data in the worker props before finishing.


      // Call Done() to signal it's finished.
      Done();
    }

    public override WorkerInstance Duplicate() => new CreateSpeckleObjectByKeyValueWorker(Converter);

    public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
    {
      DA.DisableGapLogic();
      // Use DA.GetData as usual...
      DA.GetDataList(0, keys);
      DA.GetDataTree(1, out valueTree);
      iteration = DA.Iteration;
    }

    public override void SetData(IGH_DataAccess DA)
    {
      // 👉 Checking for cancellation!
      if (CancellationToken.IsCancellationRequested) return;

      // Use DA.SetData as usual...
      DA.SetData(0, new GH_SpeckleBase {Value = speckleObj});
    }
    
    private static GH_Structure<IGH_Goo> GetSubTree(GH_Structure<IGH_Goo> valueTree, GH_Path searchPath)
    {
      var subTree = new GH_Structure<IGH_Goo>();
      var gen = 0;
      foreach (var path in valueTree.Paths)
      {
        var branch = valueTree.get_Branch(path) as IEnumerable<IGH_Goo>;
        if (path.IsAncestor(searchPath, ref gen))
        {
          subTree.AppendRange(branch, path);
        }
        else if (path.IsCoincident(searchPath))
        {
          subTree.AppendRange(branch, path);
          break;
        }
      }
      subTree.Simplify(GH_SimplificationMode.CollapseLeadingOverlaps);
      return subTree;
    }
  }
}
