using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ConnectorGrashopper.Extras;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Utilities = ConnectorGrashopper.Extras.Utilities;

namespace ConnectorGrashopper.Objects
{
  public class ExpandSpeckleObjectAsync : GH_AsyncComponent, IGH_VariableParameterComponent
  {
    public ISpeckleConverter Converter;
    public ISpeckleKit Kit;

    public List<string> outputList = new List<string>();

    public override Guid ComponentGuid => new Guid("A33BB8DF-A9C1-4CD1-855F-D6A8B277102B");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override Bitmap Icon => Properties.Resources.ExpandSpeckleObject;

    public ExpandSpeckleObjectAsync() : base("Expand Speckle Object", "ESO",
      "Allows you to decompose a Speckle object in its constituent parts.",
      "Speckle 2", "Async Object Management")
    {
      Kit = KitManager.GetDefaultKit();
      try
      {
        Converter = Kit.LoadConverter(Applications.Rhino);
        BaseWorker = new ExpandSpeckleObjectWorker(this, Converter);
        Message = $"{Kit.Name} Kit";
      }
      catch
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No default kit found on this machine.");
      }
    }

    protected override void BeforeSolveInstance()
    {
      if (outputList != null)
        AutoCreateOutputs();
      base.BeforeSolveInstance();
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleBaseParam("Speckle Object", "O",
        "Speckle object to deconstruct into it's properties.", GH_ParamAccess.tree));
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      // INFO -> All output params are dynamically generated!
    }

    public bool CanInsertParameter(GH_ParameterSide side, int index) => false;

    public bool CanRemoveParameter(GH_ParameterSide side, int index) => false;

    public IGH_Param CreateParameter(GH_ParameterSide side, int index)
    {
      var myParam = new GenericAccessParam
      {
        Name = GH_ComponentParamServer.InventUniqueNickname("ABCD", Params.Input),
        MutableNickName = true,
        Optional = true
      };

      myParam.NickName = myParam.Name;
      myParam.ObjectChanged += (sender, e) => { };

      return myParam;
    }

    public bool DestroyParameter(GH_ParameterSide side, int index)
    {
      if (side == GH_ParameterSide.Input)
        return false;
      return true;
    }

    public void VariableParameterMaintenance()
    {
      if (outputList.Count == 0) return;

      for (var i = 0; i < Params.Output.Count; i++)
      {
        if (i > outputList.Count - 1) return;

        var name = outputList[i];
        Params.Output[i].Name = $"{name}";
        Params.Output[i].NickName = $"{name}";
        Params.Output[i].Description = $"Data from property: {name}";
        Params.Output[i].MutableNickName = false;
        Params.Output[i].Access = GH_ParamAccess.tree;
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
        Menu_AppendItem(menu, $"{kit.Name} ({kit.Description})", (s, e) => { SetConverterFromKit(kit.Name); }, true,
          kit.Name == Kit.Name);
      }

      Menu_AppendSeparator(menu);
    }

    public void SetConverterFromKit(string kitName)
    {
      if (kitName == Kit.Name) return;

      Kit = KitManager.Kits.FirstOrDefault(k => k.Name == kitName);
      Converter = Kit.LoadConverter(Applications.Rhino);
      BaseWorker = new ExpandSpeckleObjectWorker(this, Converter);
      Message = $"Using the {Kit.Name} Converter";
      ExpireSolution(true);
    }

    private bool OutputMismatch()
    {
      bool countMatch = outputList.Count == Params.Output.Count;
      if (!countMatch)
        return true;

      for (var i = 0; i < outputList.Count; i++)
        if (Params.Output[i].NickName != outputList[i])
          return true;

      return false;
    }

    private void AutoCreateOutputs()
    {
      int tokenCount = outputList.Count;

      if (tokenCount == 0 || !OutputMismatch()) return;
      RecordUndoEvent("Creating Outputs");

      if (Params.Output.Count < tokenCount)
        while (Params.Output.Count < tokenCount)
        {
          var newParam = CreateParameter(GH_ParameterSide.Output, Params.Output.Count);
          Params.RegisterOutputParam(newParam);
        }
      else if (Params.Output.Count > tokenCount)
        while (Params.Output.Count > tokenCount)
        {
          Params.UnregisterOutputParameter(Params.Output[Params.Output.Count - 1]);
        }

      Params.OnParametersChanged();
      VariableParameterMaintenance();
    }
  }

  public class ExpandSpeckleObjectWorker : WorkerInstance
  {
    public ISpeckleConverter Converter;
    public GH_Structure<GH_SpeckleBase> speckleObjects;

    public Dictionary<string, GH_Structure<GH_ObjectWrapper>> outputDict;
    public GH_ComponentParamServer Params;
    public ExpandSpeckleObjectWorker(GH_Component _parent, ISpeckleConverter converter) : base(_parent)
    {
      Converter = converter;
    }

    public override WorkerInstance Duplicate() => new ExpandSpeckleObjectWorker(Parent, Converter);

    public override void DoWork(Action<string, double> ReportProgress, Action Done)
    {
      outputDict = CreateOutputDictionary();
      Done();
    }

    public override void SetData(IGH_DataAccess DA)
    {
      if (outputDict == null) return;
      
      foreach (var key in outputDict.Keys)
        DA.SetDataTree(Params.IndexOfOutputParam(key), outputDict[key]);
    }

    public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
    {
      DA.GetDataTree(0, out speckleObjects);
      speckleObjects.Graft(GH_GraftMode.GraftAll);
      this.Params = Params;
      (Parent as ExpandSpeckleObjectAsync).outputList = GetOutputList();
    }

    private List<string> GetOutputList()
    {
      // Get the full list of output parameters
      var fullProps = new List<string>();
      foreach (var path in speckleObjects.Paths)
      {
        if (speckleObjects.get_Branch(path).Count == 0) continue;
        var obj = speckleObjects.get_DataItem(path, 0);
        if (obj == null) continue;
        var b =  obj.Value;
        var props = b.GetDynamicMembers().ToList();
        props.ForEach(prop =>
        {
          if (!fullProps.Contains(prop) && b[prop] != null) 
            fullProps.Add(prop);
          else if (fullProps.Contains(prop) && b[prop] == null) 
            fullProps.Remove(prop);
        });
      }
      return fullProps;
    }
    
    private Dictionary<string, GH_Structure<GH_ObjectWrapper>> CreateOutputDictionary()
    {
      // Create empty data tree placeholders for output.
      var outputDict = GetOutputList().ToDictionary(outParam => outParam, _ => new GH_Structure<GH_ObjectWrapper>());

      // Assign all values to it's corresponding dictionary entry and branch path.
      foreach (var path in speckleObjects.Paths)
      {
        if (speckleObjects.get_Branch(path).Count == 0) continue;
        // Loop through all dynamic properties
        var baseGoo = speckleObjects.get_DataItem(path, 0) as GH_SpeckleBase;
        if (baseGoo == null)
        {
          continue;
        }
        var obj = baseGoo.Value;
        foreach (var prop in obj.GetDynamicMembers())
        {
          // Convert and add to corresponding output structure
          var value = obj[prop];
          if (!outputDict.ContainsKey(prop)) continue;
          switch (value)
          {
            case null:
              continue;
            case System.Collections.IList list:
              foreach(var x in list)
              {
                var wrapper = new GH_ObjectWrapper();
                wrapper.Value = Utilities.TryConvertItemToNative(x, Converter);
                outputDict[prop].Append(wrapper);
              }
              break;
            // TODO: Not clear how we handle "tree" inner props. They can only be set by sender components,
            // so perhaps this is not an issue. Below a simple stopgap so we can actually see what data is
            // inside a sender-created object.
            case Dictionary<string, List<Base>> dict:
              foreach(var kvp in dict)
              {
                var wrapper = new GH_ObjectWrapper();
                foreach(var b in kvp.Value)
                {
                  wrapper.Value = Utilities.TryConvertItemToNative(b, Converter);
                }
                outputDict[prop].Append(wrapper);
              }
              break;
            default:
              outputDict[prop].Append(
                new GH_ObjectWrapper(Utilities.TryConvertItemToNative(obj[prop], Converter)),
                path);
              break;
          }
        }
      }

      return outputDict;
    }

  }
}
