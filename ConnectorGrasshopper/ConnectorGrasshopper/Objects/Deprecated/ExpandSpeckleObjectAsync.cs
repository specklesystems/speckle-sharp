using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Utilities = ConnectorGrasshopper.Extras.Utilities;

namespace ConnectorGrasshopper.Objects
{
  public class ExpandSpeckleObjectAsync : SelectKitAsyncComponentBase, IGH_VariableParameterComponent
  {
    public List<string> outputList = new List<string>();
    public override Guid ComponentGuid => new Guid("A33BB8DF-A9C1-4CD1-855F-D6A8B277102B");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    protected override Bitmap Icon => Properties.Resources.ExpandSpeckleObject;
    public override bool Obsolete => true;

    public int State = 0;
    public ExpandSpeckleObjectAsync() : base("Expand Speckle Object", "ESO",
      "Allows you to decompose a Speckle object in its constituent parts.",
      ComponentCategories.PRIMARY_RIBBON, ComponentCategories.OBJECTS)
    {
    }

    public override void SetConverter()
    {
      base.SetConverter();
      BaseWorker = new ExpandSpeckleObjectWorker(this, Converter);
    }

    protected override void BeforeSolveInstance()
    {
      if (State == 1)
        AutoCreateOutputs();
      base.BeforeSolveInstance();
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      var pObj = pManager.AddParameter(new SpeckleBaseParam("Speckle Object", "O",
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
        Optional = true,
      };
      myParam.NickName = myParam.Name;
      return myParam;
    }

    public bool DestroyParameter(GH_ParameterSide side, int index) => side == GH_ParameterSide.Output;
    
    public void VariableParameterMaintenance()
    {
      // Perform parameter maintenance here!
    }

    private bool OutputMismatch() =>
      outputList.Count != Params.Output.Count 
      || outputList.Where((t, i) => Params.Output[i].NickName != t).Any();

    private void AutoCreateOutputs()
    {
      var tokenCount = outputList?.Count ?? 0 ;

      if (tokenCount == 0 || !OutputMismatch()) return;
      RecordUndoEvent("Creating Outputs");
      
      // Check what params must be deleted, and do so when safe.
      var remove = Params.Output.Select((p, i) =>
      {
        var res = outputList.Find(o => o == p.Name);
        return res == null ? i : -1;
      }).ToList();
      remove.Reverse();
      remove.ForEach(b =>
      {
        if (b != -1 && Params.Output[b].Recipients.Count == 0)
          Params.UnregisterOutputParameter(Params.Output[b]);
      });
      
      outputList.Sort();
      outputList.ForEach(s =>
      {
        var param = Params.Output.Find(p => p.Name == s);
        if (param == null)
        {
          var newParam = CreateParameter(GH_ParameterSide.Output, Params.Output.Count);
          newParam.Name = s;
          newParam.NickName = s;
          newParam.Description = $"Data from property: {s}";
          newParam.MutableNickName = false;
          newParam.Access = GH_ParamAccess.tree;
          Params.RegisterOutputParam(newParam);
        }
        
      });
      var paramNames = Params.Output.Select(p => p.Name).ToList();
      paramNames.Sort();
      var sortOrder = Params.Output.Select(p => paramNames.IndexOf(p.Name)).ToArray();
      Params.SortOutput(sortOrder);
      Params.OnParametersChanged();
      VariableParameterMaintenance();
    }
  }

  public class ExpandSpeckleObjectWorker : WorkerInstance
  {
    public ISpeckleConverter Converter;
    public GH_Structure<GH_SpeckleBase> speckleObjects;

    public Dictionary<string, GH_Structure<IGH_Goo>> outputDict;
    private List<string> outputList = new List<string>();
    public GH_ComponentParamServer Params;

    public ExpandSpeckleObjectWorker(GH_Component _parent, ISpeckleConverter converter) : base(_parent)
    {
      Converter = converter;
    }

    public override WorkerInstance Duplicate() => new ExpandSpeckleObjectWorker(Parent, Converter);
    
    public override void DoWork(Action<string, double> ReportProgress, Action Done)
    {
      try
      {
        Parent.Message = "Expanding...";
        if (speckleObjects.DataCount == 0)
        {
          Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,"Input was empty!");
          Parent.Message = "Done";
        }
        outputList = GetOutputList();
        (Parent as ExpandSpeckleObjectAsync).outputList = outputList;
        outputDict = CreateOutputDictionary();
        (Parent as ExpandSpeckleObjectAsync).State = 1;
        Done();
      }
      catch (Exception e)
      {
        // If we reach this, something happened that we weren't expecting...
        Log.CaptureException(e);
        Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Something went terribly wrong... " + e.Message);
        Parent.Message = "Error";
      }
    }

    public override void SetData(IGH_DataAccess DA)
    {
      if (outputDict == null) return;
      foreach (var key in outputDict.Keys)
      {
        var indexOfOutputParam = Params.IndexOfOutputParam(key);
        if(indexOfOutputParam != -1)
        {
          var ghStructure = outputDict[key];
          DA.SetDataTree(indexOfOutputParam, ghStructure);
        }
      }

      // Report all conversion errors as warnings
      if(Converter != null)
      {
        foreach (var error in Converter.Report.ConversionErrors)
        {
          Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
            error.Message + ": " + error.InnerException?.Message);
        }
        Converter.Report.ConversionErrors.Clear();
      }
      outputDict = null;
      (Parent as ExpandSpeckleObjectAsync).State = 0;

    }

    public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
    {
      Parent.ClearRuntimeMessages();
      DA.GetDataTree(0, out speckleObjects);
      speckleObjects.Graft(GH_GraftMode.GraftAll);
      var names = Params.Output.Select(p => p.Name);
      Console.WriteLine("Previous names:{0}", string.Join(", ",names));
      this.Params = Params;
    }

    private List<string> GetOutputList()
    {
      // Get the full list of output parameters
      var fullProps = new List<string>();
      foreach (var path in speckleObjects.Paths)
      {
        if (speckleObjects.get_Branch(path).Count == 0) continue;
        var obj = speckleObjects.get_DataItem(path, 0);
        var b = obj.Value;
        b?.GetMemberNames().ToList().ForEach(prop =>
        {
          if (!fullProps.Contains(prop) && b[prop] != null)
            fullProps.Add(prop);
          else if (fullProps.Contains(prop) && b[prop] == null)
            fullProps.Remove(prop);
        });
      }
      fullProps.Sort();
      return fullProps;
    }
    
    private Dictionary<string, GH_Structure<IGH_Goo>> CreateOutputDictionary()
    {
      // Create empty data tree placeholders for output.
      var outputDict = outputList.ToDictionary(outParam => outParam, _ => new GH_Structure<IGH_Goo>());

      // Assign all values to it's corresponding dictionary entry and branch path.
      foreach (var path in speckleObjects.Paths)
      {
        if (speckleObjects.get_Branch(path).Count == 0) continue;
        // Loop through all dynamic properties
        var baseGoo = speckleObjects.get_DataItem(path, 0) as GH_SpeckleBase;
        if (baseGoo?.Value == null)
          continue;

        var obj = baseGoo.Value;
        foreach (var prop in obj.GetMembers())
        {
          // Convert and add to corresponding output structure
          var value = prop.Value;
          if (!outputDict.ContainsKey(prop.Key)) continue;
          switch (value)
          {
            case null:
              continue;
            case System.Collections.IList list:
              var index = 0;
              foreach (var x in list)
              {
                var wrapper = Utilities.TryConvertItemToNative(x, Converter);
                outputDict[prop.Key].Append(wrapper, path);
                index++;
              }
              break;
            // TODO: Not clear how we handle "tree" inner props. They can only be set by sender components,
            // so perhaps this is not an issue. Below a simple stopgap so we can actually see what data is
            // inside a sender-created object.
            case Dictionary<string, List<Base>> dict:
              foreach (var kvp in dict)
              {
                
                IGH_Goo wrapper = new GH_ObjectWrapper();
                foreach (var b in kvp.Value)
                {
                  wrapper = Utilities.TryConvertItemToNative(b, Converter);
                }
                outputDict[prop.Key].Append(wrapper, path);
              }
              break;
            default:
              outputDict[prop.Key].Append(
                Utilities.TryConvertItemToNative(obj[prop.Key], Converter),
                path);
              break;
          }
        }
      }

      return outputDict;
    }
    
  }
}
